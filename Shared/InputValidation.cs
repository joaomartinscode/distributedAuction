using System;

namespace Utils
{
    public class InputValidation
    {
        public static int ValidateIntGE0(string message)
        {
            while (true)
            {
                Console.WriteLine(message);

                if (int.TryParse(Console.ReadLine(), out int value))
                {
                    if (value >= 0)
                    {
                        return value;
                    }
                    else
                    {
                        Console.WriteLine("Por favor, introduza um número inteiro maior ou igual a zero.");
                    }
                }
                else
                {
                    Console.WriteLine("Por favor, introduza um número inteiro maior ou igual a zero.");
                }
            }
        }

        public static int ValidateIntBetween(string message, int min, int max)
        {
            Console.Write(message);

            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out int value))
                {
                    if (value >= min && value <= max)
                    {
                        return value;
                    }
                    else
                    {
                        Console.WriteLine("Por favor, introduza um número inteiro entre " + min + " e " + max);
                    }
                }
                else
                {
                    Console.WriteLine("Por favor, introduza um número inteiro entre " + min + " e " + max);
                }
            }
        }

        public static int ValidateIntGT0(string text)
        {
            while (true)
            {
                Console.Write(text);

                if (int.TryParse(Console.ReadLine(), out int value))
                {
                    if (value <= 0)
                    {
                        Console.WriteLine("Por favor, introduza um número inteiro maior do que 0.");
                    }
                    else
                    {
                        return value;
                    }
                }
                else
                {
                    Console.WriteLine("Por favor, introduza um número inteiro maior do que 0.");
                }
            }
        }

        public static int ValidateIntGTN(string text, int n)
        {
            while (true)
            {
                Console.Write(text);

                if (int.TryParse(Console.ReadLine(), out int value))
                {
                    if (value <= n)
                    {
                        Console.WriteLine("Por favor, introduza um número inteiro maior do que " + n + ".");
                    }
                    else
                    {
                        return value;
                    }
                }
                else
                {
                    Console.WriteLine("Por favor, introduza um número inteiro maior do que 0 " + n + ".");
                }
            }
        }

        public static int ValidateInt(string text)
        {
            while (true)
            {
                Console.Write(text);

                if (int.TryParse(Console.ReadLine(), out int value))
                {
                    return value;
                }
                else
                {
                    Console.WriteLine("Por favor introduza um número inteiro maior do que 0.");
                }
            }
        }

        public static double ValidateDoubleGT0(string text)
        {
            while (true)
            {
                Console.Write(text);

                if (double.TryParse(Console.ReadLine(), out double value))
                {
                    if (value < 0)
                    {
                        Console.WriteLine("Por favor introduza um double maior do que 0.");
                    }
                    else
                    {
                        return value;
                    }
                }
                else
                {
                    Console.WriteLine("Por favor introduza um double maior do que 0");
                }
            }
        }
    }
}
