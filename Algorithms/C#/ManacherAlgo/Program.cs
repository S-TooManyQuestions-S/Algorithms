using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AndrewSamarenko_191_3
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            List<string> tests = FileReader(args[0]);
            List<string> answers = new List<string>(0);
            int even, odd;
            foreach (var word in tests)
            {
                even = Sum(EvenPoly(word));
                odd = Sum(OddPoly(word));
                answers.Add(String.Format("{0} {1} {2}", even+odd, even, odd));
            }
            FileWriter(args[1], answers);
            
        }

        private static void FileWriter(string path, List<string> answers)
        {
            try
            {
                using(FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        foreach (var VARIABLE in answers)
                        {
                            writer.WriteLine(VARIABLE);
                        }
                    }

            }
            catch (IOException e)
            {
                Console.WriteLine("Ошибка при работе с файлом: \n" + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("Недостаточно прав для доступа к файлу: \n" + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Непредвиденная ошибка: \n"+e.Message);
            }
        }

        private static int Sum(int[] array)
        {
            int sum = 0;
            foreach (var VARIABLE in array)
                sum += VARIABLE;
            return sum;
        }
        private static List<string> FileReader(string path)
        {
            try
            {
                List<string> info;
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (StreamReader reader = new StreamReader(stream))
                {
                    info = reader.ReadToEnd().Split(new string[] {"\n"}, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }

                return info;
            }
            catch (IOException e)
            {
                Console.WriteLine("Ошибка при работе с файлом: \n" + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("Недостаточно прав для доступа к файлу: \n" + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Непредвиденная ошибка: \n"+e.Message);
            }

            return null;
        }

        /// <summary>
        /// Метод для получения количества палиндромов нечетной длины
        /// для заданного слова
        /// </summary>
        /// <param name="word">слова, в котором необхордимо найти палиндромы
        /// нечетной длины</param>
        /// <returns>вектор содержащий количество палиндромов нечетной длины для каждой позиции слова</returns>
        private static int[] OddPoly(string word)
        {
            //границы самого правого палиндрома нашей строки
            //левая граница
            int left = 0;
            //правая граница
            int right = -1;
            //длина заданной строки 
            int n = word.Length;
            //количество найденных палиндромов для конкретной позиции i
            int amount;
            //вектор для хранения количества палиндромов (vector[i]) позиции i слова word
            int[] vector = new int[n];
            //проходимся по слову в поисках палиндромов (по каждой позиции)
            for (int i = 0; i < n; ++i)
            {
                //если рассматриваемая позиция находится правее нашего самого правого палиндрома
                //то мы присваиваем количеству (k) палиндромов для нашей позиции 1, т.к. сама буква
                //уже является палиндромом
                if (i > right)
                    amount = 1;
                else
                    //здесь необходимо обрабатывать ситуации, когда поиск палиндрома выходит за границы
                    //нашего правого палиндрома, то есть случаи, когда "отражение" не будет работать, где мы
                    //не можем гарантировать действенность отражения (правильность, т.е. 
                    //не можем гарантировать, что количество палиндромов в позиции i будет таким же как и в 
                    //в позиции left+right-i) чтобы исключить данную ошибку - обрезаем наш вектор по необходимости (находим минимум)
                    amount = Math.Min(vector[left+right-i], right-i+1);
                
                while (i + amount < n && //не выходит за правую границу начального слова
                       i - amount >= 0 && //не выходит за левую границу начального слова
                       word[i + amount] == word[i - amount]) //тривиальный алгоритм - проверяем, что 
                    //левый символ == правому символу (определяем палиндром или нет)
                    //если все верно - увеличиваем количество палиндромов для позиции
                    amount++;
                //присваиваем найденное количество к соответственной позиции нашего вектора
                vector[i] = amount;
                //если палиндром правее нашего - обновляем границы нашего палиндрома на новые
                if (i + amount - 1 > right)
                {
                    right = i + amount - 1;
                    left = i - amount + 1;
                }
            }
            //возвращаем информацию о количестве палиндромов для каждой буквы (позиции заданного слова)
            return vector;
        }

        private static int[] EvenPoly(string word)
        {
            //границы самого правого палиндрома нашей строки
            //левая граница
            int left = 0;
            //правая граница
            int right = -1;
            //длина заданной строки 
            int n = word.Length;
            //количество найденных палиндромов для конкретной позиции i
            int amount;
            //вектор для хранения количества палиндромов (vector[i]) позиции i слова word
            int[] vector = new int[n];
            //проходимся по слову в поисках палиндромов (по каждой позиции)
            for (int i = 0; i < n; ++i)
            {
                //в данном случае символ не считается за палиндром
                if (i > right)
                    amount = 0;
                else
                    //отражение в данном случае работает немного иначе (нужен сдвиг на +1)
                    amount = Math.Min(vector[left+right-i+1], right-i+1);
                
                while (i + amount < n && //не выходит за правую границу начального слова
                       i - amount - 1 >= 0 && //не выходит за левую границу начального слова
                       word[i+amount] == word[i-amount-1]) //тривиальный алгоритм - проверяем, что 
                    //левый символ == правому символу (определяем палиндром или нет)
                    //если все верно - увеличиваем количество палиндромов для позиции
                    amount++;
                //присваиваем найденное количество к соответственной позиции нашего вектора
                vector[i] = amount;
                //если палиндром правее нашего - обновляем границы нашего палиндрома на новые
                if (i + amount - 1 > right)
                {
                    right = i + amount - 1 ;
                    left = i - amount;
                }
            }
            //возвращаем информацию о количестве палиндромов для каждой буквы (позиции заданного слова)
            return vector;
        }
    }
}