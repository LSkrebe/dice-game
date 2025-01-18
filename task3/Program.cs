using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ConsoleTables;

public class Dice
{
    public int[] Values { get; private set; }

    public Dice(string valuesString)
    {
        Values = valuesString.Split(',').Select(int.Parse).ToArray();
        if (Values.Length != 6)
            throw new ArgumentException("Each dice must have exactly 6 integers.");
    }

    public override string ToString()
    {
        return "[" + string.Join(",", Values) + "]";
    }
}

public class DiceGame
{
    private List<Dice> diceSet;
    private RandomNumberGenerator rng;

    public DiceGame(List<Dice> diceSet)
    {
        this.diceSet = diceSet;
        this.rng = RandomNumberGenerator.Create();
    }

    private byte[] GenerateRandomKey()
    {
        byte[] key = new byte[32]; // 256 bits
        rng.GetBytes(key);
        return key;
    }

    private byte[] GenerateHMAC(int value, byte[] key)
    {
        using (var hmac = new HMACSHA256(key))
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(value.ToString());
            return hmac.ComputeHash(valueBytes);
        }
    }

    public int FairRandomGenerate(int maxValue, out byte[] hmacKey)
    {
        hmacKey = GenerateRandomKey();
        int randomValue;
        do
        {
            byte[] randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            randomValue = BitConverter.ToInt32(randomBytes, 0) & int.MaxValue;
        } while (randomValue > int.MaxValue - (int.MaxValue % (maxValue + 1)));

        randomValue %= (maxValue + 1);

        byte[] hmac = GenerateHMAC(randomValue, hmacKey);
        Console.WriteLine($"HMAC={BitConverter.ToString(hmac).Replace("-", "")}");
        return randomValue;
    }

    public bool DetermineFirstMove()
    {
        Console.WriteLine("Let's determine who makes the first move.");
        int randomBit = FairRandomGenerate(1, out byte[] hmacKey);
        Console.WriteLine($"I selected a random value in the range 0..1 (HMAC={BitConverter.ToString(GenerateHMAC(randomBit, hmacKey)).Replace("-", "")}).");

        Console.WriteLine("Try to guess my selection:");
        Console.WriteLine("0 - 0");
        Console.WriteLine("1 - 1");
        Console.WriteLine("X - exit");
        Console.WriteLine("? - help");

        while (true)
        {
            Console.Write("Your selection: ");
            string? userInput = Console.ReadLine()?.Trim().ToLower();

            if (userInput == "x")
            {
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            }
            else if (userInput == "?")
            {
                DisplayHelp();
            }
            else if (int.TryParse(userInput, out int userGuess) && (userGuess == 0 || userGuess == 1))
            {
                Console.WriteLine($"My selection: {randomBit} (KEY={BitConverter.ToString(hmacKey).Replace("-", "")}).");
                return userGuess != randomBit;
            }
            else
            {
                Console.WriteLine("Invalid selection. Please enter 0 or 1.");
            }
        }
    }

    public void Play()
    {
        bool computerFirst = DetermineFirstMove();

        Dice computerDice, userDice;
        if (computerFirst)
        {
            Console.WriteLine($"I make the first move and choose the {computerDice = SelectRandomDice()} dice.");

            diceSet.Remove(computerDice);

            Console.WriteLine($"You choose the {userDice = SelectDice()} dice.");
        }
        else
        {
            Console.WriteLine($"You make the first move and choose the {userDice = SelectDice()} dice.");

            diceSet.Remove(userDice);

            Console.WriteLine($"I choose the {computerDice = SelectRandomDice()} dice.");
        }

        int computerThrow = MakeThrow(computerDice, "My");
        int userThrow = MakeThrow(userDice, "Your");

        if (userThrow > computerThrow)
            Console.WriteLine($"You win! ({userThrow} > {computerThrow})");
        else if (computerThrow > userThrow)
            Console.WriteLine($"I win! ({userThrow} < {computerThrow})");
        else
            Console.WriteLine($"It's a draw! ({userThrow} = {computerThrow})");
    }

    private Dice SelectRandomDice()
    {
        Random random = new Random();
        int randomIndex = random.Next(0, diceSet.Count);
        return diceSet[randomIndex];
    }

    private Dice SelectDice()
    {

        Console.WriteLine("Choose your dice:");
        for (int i = 0; i < diceSet.Count; i++)
        {
            Console.WriteLine($"{i} - {diceSet[i]}");
        }
        Console.WriteLine("X - exit");
        Console.WriteLine("? - help");

        while (true)
        {
            Console.Write("Your selection: ");
            string? userInput = Console.ReadLine()?.Trim().ToLower();

            if (userInput == "x")
            {
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            }
            else if (userInput == "?")
            {
                DisplayHelp();
            }
            else if (int.TryParse(userInput, out int selection) && selection >= 0 && selection < diceSet.Count)
            {
                return diceSet[selection];
            }
            else
            {
                Console.WriteLine("Invalid selection. Please choose a valid option.");
            }
        }
    }

    private int MakeThrow(Dice dice, string playerName)
    {
        Console.WriteLine($"It's time for {playerName.ToLower()} throw.");
        Console.WriteLine("I selected a random value in the range 0..5");
        int randomValue = FairRandomGenerate(dice.Values.Length - 1, out byte[] hmacKey);

        Console.WriteLine("Add your number modulo 6.");
        for (int i = 0; i < 6; i++)
        {
            Console.WriteLine($"{i} - {i}");
        }
        Console.WriteLine("X - exit");
        Console.WriteLine("? - help");

        while (true)
        {
            Console.Write("Your selection: ");
            string? userInput = Console.ReadLine()?.Trim().ToLower();

            if (userInput == "x")
            {
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            }
            else if (userInput == "?")
            {
                DisplayHelp();
            }
            else if (int.TryParse(userInput, out int selection) && selection >= 0 && selection < 6)
            {
                Console.WriteLine($"My number is {randomValue} (KEY={BitConverter.ToString(hmacKey).Replace("-", "")}).");

                int throwIndex = (randomValue + selection) % 6;
                int throwValue = dice.Values[throwIndex];
                Console.WriteLine($"The result is {randomValue} + {selection} = {throwIndex} (mod 6).");

                Console.WriteLine($"{playerName} throw is {throwValue}.");

                return throwValue;
            }
            else
            {
                Console.WriteLine("Invalid selection. Please choose a valid option.");
            }
        }
    }

    private void DisplayProbabilitiesTable()
    {
        Console.WriteLine("Calculating probabilities...");

        var table = new ConsoleTable("Dice 1", "Dice 2", "Dice 1 Wins %", "Dice 2 Wins %", "Draw %");

        for (int i = 0; i < diceSet.Count; i++)
        {
            for (int j = 0; j < diceSet.Count; j++)
            {
                if (i != j)
                {
                    var (wins1, wins2, draws) = CalculateProbabilities(diceSet[i], diceSet[j]);
                    table.AddRow(diceSet[i].ToString(), diceSet[j].ToString(), wins1, wins2, draws);
                }
            }
        }

        table.Write(Format.Alternative);
    }

    private (double, double, double) CalculateProbabilities(Dice dice1, Dice dice2)
    {
        int wins1 = 0, wins2 = 0, draws = 0;
        int totalRolls = dice1.Values.Length * dice2.Values.Length;

        foreach (int value1 in dice1.Values)
        {
            foreach (int value2 in dice2.Values)
            {
                if (value1 > value2)
                    wins1++;
                else if (value1 < value2)
                    wins2++;
                else
                    draws++;
            }
        }

        return (
            Math.Round((double)wins1 / totalRolls * 100, 2),
            Math.Round((double)wins2 / totalRolls * 100, 2),
            Math.Round((double)draws / totalRolls * 100, 2)
        );
    }

    private void DisplayHelp()
    {
        Console.WriteLine("Help: This is a non-transitive dice game.");
        Console.WriteLine("The table below shows the probabilities of winning for each pair of dice:");
        DisplayProbabilitiesTable();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Incorrect number of arguments. You must provide at least 3 dice configurations.");
            Console.WriteLine("Example: dotnet run 2,2,4,4,9,9 6,8,1,1,8,6 7,5,3,7,5,3");
            return;
        }

        try
        {
            List<Dice> diceSet = args.Select(config => new Dice(config)).ToList();
            DiceGame game = new DiceGame(diceSet);
            game.Play();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
