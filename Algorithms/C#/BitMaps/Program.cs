using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;


namespace RoaringBitMaps
{
    internal class Program
    {
        internal static string dataPath;
        public static void Main(string[] args)
        {
            string[] commands = File.ReadAllLines($"{args[1]}");
            dataPath = args[0];
            //список RoaringBitmap, составленных на основе полученных данных (запросах)
            var bitmaps = new List<RoaringBitMap>()
            {
                Parser.BitMapByKeys(Parser.DimParse(new []{"DimPromotion.MinQty","<>","-1"}), Parser.FactoryConnections["DimPromotion"])
            };
                //замер времени работы алгоритма
            Stopwatch all = new Stopwatch();
            all.Start();
//            Stopwatch stop = new Stopwatch();
//            var pathsForOutputColumn = in[0].Split(new[] {","}, StringSplitOptions.None);
//            var numberOfCommands = int.Parse(args[1]);Console.WriteLine(stop.Elapsed.ToString());

            uint numberOfCommands = uint.Parse(commands[1]);
            //обработка каждый команды (построчно)
            for (int i = 0; i < numberOfCommands; i++)
            {
                //cчитывание команды
                var line = commands[i+2];
                //получение массива команд
                var arrayOfCommands = line?.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
                //костыль (если вдруг имеются пробелы в названии для сравнения)
                for (int j = 3; j < arrayOfCommands.Length; j++)
                {
                    arrayOfCommands[2] += " " + arrayOfCommands[j];
                }
                
                //если нам запрос к подтаблице (Dim)
                if (arrayOfCommands[0].Contains("Dim"))
                {
                    var keys = Parser.DimParse(arrayOfCommands);
                    var tables = arrayOfCommands[0].Split('.');
                    bitmaps.Add(Parser.BitMapByKeys(keys, Parser.FactoryConnections[tables[0]]));
//                    stop.Stop();
//                    Console.WriteLine(i+ " Dim " + stop.Elapsed.ToString());
                }
                //если запрос к таблице фактов (Fact)
                else
                {
                    bitmaps.Add(Parser.FactresellerSalesParse(arrayOfCommands));
//                    stop.Stop();
//                    Console.WriteLine(i+ " Fact " + stop.Elapsed.ToString());
                }
            }
            //пересечение всех RoaringBitmap, результат сохраняется в первой ячейке
                for (int i = 1; i < bitmaps.Count; i++)
                {
                    //пересечение всех битмап
                    bitmaps[0].And(bitmaps[i]);
                }
            
            //хранение ответов
            List<List<string>> outputStrings = new List<List<string>>();
            
            FindThemAll(outputStrings, bitmaps[0], commands);
            
            //вывод ответов
            Parser.OutPut(outputStrings,args[2]);
            all.Stop();
            Console.WriteLine("Total Elapsed Time " + all.Elapsed);
            //Console.WriteLine(File.ReadAllText(args[2]).Equals(File.ReadAllText(realAnswer)));
        }
        
        /// <summary>
        /// Инициализация списка
        /// </summary>
        /// <param name="array">Массив, который необходимо проинициализировать</param>
        /// <param name="size">Размер массива для инициализации</param>
        internal static void Initialization(List<List<string>> array, int size)
        {
            for (int i = 0; i < size; i++)
                array.Add(new List<string>());
        }


        private static void FindThemAll(List<List<string>> outputStrings, RoaringBitMap bitmap, string[] commands)
        {
            //получение столбцов, которые необходимо вывести (ссылки на таблицы)
            string[] outputFiles = commands[0].Split(',');
            //для каждого столбца пприменяем алгоритм поиска и сохранения данных
            foreach (var output in outputFiles)
            {
                //получение таблицы и соответствующей колонки из запроса e.g. [DimPromotion, PromotionAlternateKey]
                string[] tableAndColumn = output.Split('.');
                //лист, в который записываются подходящие элементы из столбца (согласно RoaringBitMap)
                List<string> lines;
                //если нам необходимо пройтись по таблице фактов, а не по подтаблице - обрабатываем
                //эту ситуацию отдельно
                if (tableAndColumn[0].Contains("FactResellerSales"))
                {
                    lines = Parser.ReadFactResellerForOutput(bitmap, output);
                }
                else{
                    lines = Parser.ReadDimForOutput(bitmap, output);
                }
                //если список пуст - инициализируем
                if (outputStrings.Count == 0)
                    Initialization(outputStrings, lines.Count);
                //добавляем наш список ответами, один лист - последовательность элементов (из каждого столбца) 
                //1-ый столбец - 0-ой элемент в списке, и так далее
                for (int i = 0; i < lines.Count; i++)
                    outputStrings[i].Add(lines[i]);
            }
        }
    }
    
    
    #region Bitmap
    /// <summary>
    /// Абстрактный класс для представления контейнера
    /// </summary>
    public abstract class Container
    {
        /// <summary>
        /// Индексатор для добавления или получения элемента из контейнера
        /// </summary>
        /// <param name="index">Индекс элемента для занесения или получения (проверки)</param>
        internal abstract bool this[uint index] { get; set; }

        /// <summary>
        /// Количество единиц в контейнере
        /// </summary>
        public abstract int getLength { get; }
        /// <summary>
        /// Операция пересечения для двух контейнеров (переопределена в каждом классе-наследнике)
        /// </summary>
        /// <param name="container">Контейнер с которым необходимо пересечь текущий</param>
        /// <returns>Контейнер, представляющий пересечение данного и переданного контейнеров</returns>

        public abstract Container And(Container container);
    }

    public class BitmapContainer : Container
    {
        /// <summary>
        /// </summary>
        /// <param name="numbers">Лист индексов на которых стоит единица</param>
        public BitmapContainer(ushort[] indexes)
        {
            //добавление индексов в BitMap (преобразование массива индексов в Bitmap)
            foreach (var rank in indexes)
            {
                bitMap[rank / 64] |= 1ul << (rank % 64);
            }
            //количество единиц
            size = indexes.Length;
        }
        //количество единиц в Bitmap
        private int size;
        /// <summary>
        /// Получение количества единиц в Bitmap (свойство)
        /// </summary>
        public override int getLength
        {
            get => size;
        }
        /// <summary>
        /// Непосредственно сам контейнер для хранения Bitmap
        /// </summary>
        private ulong[] bitMap = new ulong [1024];
        /// <summary>
        /// Индексатор для обращения к конетйнеру Bitmap
        /// </summary>
        /// <param name="index">Индекс для обращения</param>
        internal override bool this[uint index]
        {
            get
            {
                //определяем индекс в контейнере 
                ulong j = index % 64;
                //обращаемся к конкретному контейнеру (index/64) по конкретному индексу (index%64)
                //если боьше 0 => 1 == true - в данном бите содержится единица, иначе не содержится
                return (bitMap[index / 64] & (1ul << (int)j)) > 0;
            }
            set
            {
                //если на данном бите нет единицы, то мы добавляем бит
                if (!this[index])
                {
                    if (value)
                    {   
                        //добавление бита на индекс
                        bitMap[index / 64] |= 1ul <<  (int)(index % 64);
                        //увеличение количества единиц в BitMap
                        size++;
                    }
                }
                else
                {
                    if (!value)
                    {
                        //извлекаем элемент (обнуляем)
                        bitMap[index / 64] &= ~(1ul << (int)(index % 64));
                        //уменьшаем количество единиц в BitMap
                        size--;
                    }
                }
            }
        }

        /// <summary>
        /// Получение количества единиц в контейнере BitMap (в одном лонге)
        /// </summary>
        /// <param name="word">Контейнер (long)</param>
        /// <returns>Количество единиц в данном контейнере</returns>
        private static int getOnesInLong(ulong word)
        {
            int counter = 0;
            for (short i = 0; i < 64; i++)
            {
                //делим с остатком на 2, получая последний бит
                counter += (int) (word % 2);
                //отсекаем бит
                word /= 2;
            }
            return counter;
        }

        /// <summary>
        /// Переопределенный метод для пересечения двух Контейнеров (BitMap)
        /// Если количество единиц меньше чем 4096 - преобразуем данный контейнер в ArrayContainer
        /// Иначе возвращаем данный
        /// </summary>
        /// <param name="other">Контейнер для пересечения с данным</param> 
        /// <returns>Контейнер, полученный в результате пересечения двух контейнеров</returns>
        public override Container And(Container other)
        {
            if (other is BitmapContainer otherBitmapContainer)
            {
                int c = 0;
                for (int i = 0; i < 1024; i++)
                {
                    //подсчет единиц
                    c += getOnesInLong(bitMap[i] & otherBitmapContainer.bitMap[i]);
                    //пересечение двух контейнеров
                    bitMap[i] = bitMap[i] & otherBitmapContainer.bitMap[i];
                }
                //проверка на количество единиц

                if (c > 4096)
                {
                    size = c;
                    return this;
                }

                return ToArrayContainer();
            }

            return null;
        }

        /// <summary>
        /// Метод для преобразования BitMap в ArrayContainer
        /// </summary>
        /// <returns>ArrayContainer, преобразованный из текущей Bitmap'ы</returns>
        public ArrayConntainer ToArrayContainer()
        {
            var arrayContainer = new ArrayConntainer();
            for (uint i = 0; i < 65536; i++)
            {
                if (this[i])
                    arrayContainer[i] = true;
            }
            return arrayContainer;
        }
    }

    /// <summary>
    /// Класс, представляющий собой конейнер ArrayContainer
    /// (индексов единиц в Bitmap)
    /// </summary>
    public class ArrayConntainer : Container
    {
        //не может превышать 2^12 степени 
        //непосредственно сам контейнер индексов
        private ushort[] indexes;
        /// <summary>
        /// Свойство для получения количества единиц в контейнере
        /// </summary>
        public override int getLength
        {
            get => indexes.Length;
        }

        /// <summary>
        /// Индексатор для обращения к контейнеру
        ///     1. Получения конкретного индекса (его содержания - true/ отсуствия - false)
        ///     2. Добавление элемента по индексу
        ///     3. Удаление элемента по индексу
        /// </summary>
        /// <param name="index">Индекс для поиска или добавления элемента</param>
        internal override bool this[uint index]
        {
            get
            {
                return Array.BinarySearch(indexes, (ushort) index) >= 0;
            }
            set
            {
                if (this[index])
                {
                    if (!value)
                    {
                        //сортируем только перед бинарным поиском
                        Array.Sort(indexes);
                        //находим элемент для извлечения
                        ushort ourIndex = (ushort) Array.BinarySearch(indexes, (ushort) index);
                        for (ushort i = ourIndex; i < getLength - 1; i++)
                        {
                            //сдвигаем массив в цикле
                            indexes[i] = indexes[i + 1];
                        }
                        //меняем размер массива на -1 ячейку от исходного
                        Array.Resize(ref indexes, (getLength - 1));
                    }
                }
                else
                {
                    if (value)
                    {    
                        //добавление элемента в массив
                        //выделение места (рейсайз массива на +1 место от изначального)
                        Array.Resize(ref indexes, getLength + 1);
                        //добавление
                        indexes[getLength - 1] = (ushort) index;
                        
                    }
                }
            }
        }
    
        /// <summary>
        /// Стандартный конструктор для инициаилизации пустого ArrayContainer
        /// </summary>
        public ArrayConntainer()
        {
            indexes = new ushort[0];
        }

        /// <summary>
        /// Пересечение двух arrayContainer'ов
        /// </summary>
        /// <param name="other">Контейнер, с которым необходимо провести пересечение</param>
        /// <returns>Контейнер, полученный в резлуьтате пересечения данного контейнера и переданного
        /// в качестве аргумента</returns>
        public override Container And(Container other)
        {
            for(uint i = 0; i < getLength; i++)
            {
                //если в переданном контейнере нет данного индекса - зануляем его и в первом
                if (!other[indexes[i]])
                {
                    //зануление
                    this[indexes[i]] = false;
                    //смещаем итерацию на один назад
                    i--;
                }
            }
            //не обязательно, но возвращаем данный контейнер в качестве результата функции
            return this;
            //ВАЖНО: Здесь мы не можем перейти в BitMap контейнер - нет смысла проверять количество единиц
        }
        /// <summary>
        /// Преобразование в битмап-контейнер (происходит непосредственно в конструкторе BitMapContainer)
        /// </summary>
        /// <returns>Новый BitMap контейнер составленный на основе индексов старого ArrayContainer</returns>
        public BitmapContainer ToBitmapContainer()
            => new BitmapContainer(indexes);
    }

    public abstract class Bitmap
    {
        /// <summary>
        /// Перемножение двух Битмапов
        /// </summary>
        /// <param name="other">Битмап для перемножения</param>
        internal abstract void And(Bitmap other);

        /// <summary>
        /// Вставка элемента в Битмап
        /// </summary>
        /// <param name="i">Индекс элемента (глобальное)</param>
        /// <param name="value">Значение элемента</param>
        internal abstract void Set(uint i, bool value);

        /// <summary>
        /// Получение значения элемента (True - единица, False - ноль)
        /// </summary>
        /// <param name="i">Индекс элемента</param>
        /// <returns></returns>
        internal abstract bool Get(uint i);
    }

    /// <summary>
    /// Класс, представляющий RoaringBitmap
    /// </summary>
    public class RoaringBitMap : Bitmap
    {
        //контейнер bitmap'ов
        protected Container[] bitMapsContainers;

        /// <summary>
        /// Метод для перемножения двух RoaringBitmap'ов
        /// </summary>
        /// <param name="other">Roaring Bitmap для перемножения с текущим</param>
        internal override void And(Bitmap other)
        {
            //приведение к Битмапу
            if (other is RoaringBitMap otherBitmap && bitMapsContainers.Length != 0 &&
                otherBitmap.bitMapsContainers.Length != 0)
            {
                //поиск максимального ключа среди двух битмапов
                int minNumberOfKeys = Math.Min(bitMapsContainers.Length, otherBitmap.bitMapsContainers.Length);
                //обнуляем элементы, которые точно будут нулевыми
                Array.Resize(ref bitMapsContainers, minNumberOfKeys);
                //проход по ключам (ВКЛЮЧИТЕЛЬНО)
                for (uint i = 0; i < minNumberOfKeys; i++)
                {
                    //если ключ контейнер с ключом содержится и там и там - выполняем слияние
                    if (bitMapsContainers[i] != null && otherBitmap.bitMapsContainers[i] != null)
                    {
                        //если контейнер типа БитмапКонтейнер - вызываем соответствующий метод
                        //(БитмапКонтейнер всегда в аргументы)
                        if (bitMapsContainers[i] is BitmapContainer)
                            bitMapsContainers[i] = otherBitmap.bitMapsContainers[i].And(bitMapsContainers[i]);
                        else
                            bitMapsContainers[i] = bitMapsContainers[i].And(otherBitmap.bitMapsContainers[i]);
                    }
                    else
                    {
                        //иначе извлекаем
                        bitMapsContainers[i] = null;
                    }
                }
            }
            else
            {
                bitMapsContainers = new Container[0];
            }
        }

        /// <summary>
        /// Метод для помещения/извлечения элемента в/из Roaring BitMap 
        /// </summary>
        /// <param name="i">Индекс элемента</param>
        /// <param name="value">Значение (true - 1, false = 0)</param>
        internal override void Set(uint i, bool value)
        {
            //вычисление номера контейнера по индексу
            var indexOfContainer = (ushort) (i / 65536);
            //вычисление индекса в контейнере
            var indexOfElement = (ushort) (i % 65536);

            //добавляя контейнеры к нашему массиву контейнеров - мы увеличиваем длину на indexOfContainers+1
            if (indexOfContainer >= bitMapsContainers.Length)
                Array.Resize(ref bitMapsContainers, indexOfContainer + 1);
            
            //если такого контейнера не существует - создаем минимально возможный
            if (bitMapsContainers[indexOfContainer] == null)
                bitMapsContainers[indexOfContainer] = new ArrayConntainer();
            //непосредственно помещаем элемент
            bitMapsContainers[indexOfContainer][indexOfElement] = value;

            //проверка - следует ли нам менять тип контейнера на более мелкий, более обширный в зависимости
            //от количества единиц
            if (bitMapsContainers[indexOfContainer] is ArrayConntainer &&
                bitMapsContainers[indexOfContainer].getLength > 4096)
                bitMapsContainers[indexOfContainer] =
                    ((ArrayConntainer) bitMapsContainers[indexOfContainer]).ToBitmapContainer();
            else if (bitMapsContainers[indexOfContainer] is BitmapContainer &&
                     bitMapsContainers[indexOfContainer].getLength <= 4096)
                bitMapsContainers[indexOfContainer] =
                    ((BitmapContainer) bitMapsContainers[indexOfContainer]).ToArrayContainer();

        }

        /// <summary>
        /// Стандратный контейнер для инициализации нашей Roaring Bitmap
        /// </summary>
        public RoaringBitMap()
        {
            bitMapsContainers = new Container[0];
        }

        /// <summary>
        /// Получение элемента из RoaringBitmap по индексу
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        internal override bool Get(uint i)
        {
            if (i / 65536 >= bitMapsContainers.Length || bitMapsContainers[i/65536] is null)
                return false;
            return bitMapsContainers[i / 65536][(ushort) (i % 65536)];
        }
    }

    #endregion

    /// <summary>
    /// Класс, содержащий логику взаимодействия с датасетом
    /// </summary>
    static class Parser
    {
        /// <summary>
        /// Парсинг подтаблицы
        /// </summary>
        /// <param name="arrayOfArguments">Команда в строке</param>
        /// <returns>Список индексов для последующего пробегания по FactResellerSales</returns>
        public static HashSet<string> DimParse(string[] arrayOfArguments)
        {
            //получаем путь до определенной подтаблицы [DimPromotion; Qty] - например
            var pathToTable = arrayOfArguments[0].Split('.');
            //лист для ответа 
            var answer = new HashSet<string>();
            //открытие файла для чтения (для выбора таблицы парсим первое значение (до точки))
            using(FileStream file = new FileStream($"{Program.dataPath}/{pathToTable[0]}.csv",FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(file))
            {
                //если мы сталкиваемся с сравнением строк - применяем данную конструкцию, чтобы избежать лишнего парсинга
                if (arrayOfArguments[2].Contains("'"))
                {
                    //убираем ненужные кавычки для сравнения
                    string valueWithout = arrayOfArguments[2].Replace("'","");
                    //начинаем читать построчки
                    while (!reader.EndOfStream)
                    {
                        //чтение строчки в файле (разделяем, чтобы получить конкретный столбец)
                        string[] tableLine = reader.ReadLine().Split('|');
                        //получение значения в конкретном столбце 
                        string requiredValue = tableLine[Tables[pathToTable[0]] //получение структуры таблицы по словарю
                            .IndexOf(pathToTable[1])]; //поиск по листу интересующего нас столбца
                        //сравниваем строки
                        if (getCompareStrings(arrayOfArguments[1], valueWithout,
                            requiredValue))
                        {
                            //если все успешно - добавляем ИНДЕКС строки
                            answer.Add(tableLine[0]);
                        }
                    }
                }
                else
                {
                    //другая реализация, тут сравнение с числами (избегаем множественного парсинга)
                    while (!reader.EndOfStream)
                    {
                        //чтение строчки в файле
                        string[] tableLine = reader.ReadLine().Split('|');
                        //получение значения в конкретном столбце 
                        int requiredValue = int.Parse(tableLine[Tables[pathToTable[0]].IndexOf(pathToTable[1])]);
                        //если сравнение строк прошло успешно
                        if (getCompareInt(arrayOfArguments[1], int.Parse(arrayOfArguments[2]),
                            requiredValue)) //парсим значение для сравнения один раз и больше никогда
                        {
                            //если сравнение прошло успешно - дорбаваляем
                            answer.Add(tableLine[0]);
                        }
                    }
                }
            }

            return answer;
        }
        
        /// <summary>
        /// Получения RoaringBitmap по коллекции ключей дочерней таблицы из главной
        /// </summary>
        /// <param name="keys">Коллекция ключей дочерней таблицы</param>
        /// <param name="table">Таблица главной FactResellerSales</param>
        /// <returns>Составленная по ключам битмапа</returns>
        public static RoaringBitMap BitMapByKeys(HashSet<string> keys, string table)
        {
            RoaringBitMap bitmap = new RoaringBitMap();
            using(FileStream factTable =  new FileStream($"{Program.dataPath}/FactResellerSales.{table}.csv"
                , FileMode.Open,FileAccess.Read))
            using (StreamReader reader = new StreamReader(factTable))
            {
                uint index = 1;
               // Stopwatch timer = new Stopwatch();
               // timer.Start();
                while (!reader.EndOfStream)
                {
                    
                    string[] tableLine = reader.ReadLine().Split('|');
                    //если индекс содержится в ключах - добавляем индекс этого ключа
                    if(keys.Contains(tableLine[0]))
                        bitmap.Set(index, true);
                   
                    index++;
                }
                // Console.WriteLine(timer.Elapsed.ToString());
            }
            return bitmap;
        }

        /// <summary>
        /// Составление Битмап на основе данных из основной таблицы и команды пользователя (FactReseller)
        /// </summary>
        /// <param name="arrayOfArguments">Команда пользователя (массив элементов)</param>
        /// <returns>Составленная RoaringBitMap'a на основе переданных предикатов и команды</returns>
        public static RoaringBitMap FactresellerSalesParse(string[] arrayOfArguments)
        {
            RoaringBitMap bitmap = new RoaringBitMap();
            using(FileStream file = new FileStream($"{Program.dataPath}/{arrayOfArguments[0]}.csv",
                FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(file))
            {
                uint index = 1;
                //если мы сталкиваемся с сравнением строк - применяем данную конструкцию, чтобы избежать лишнего парсинга
                if (arrayOfArguments[2].Contains("'"))
                {
                    //убираем ненужные кавычки для сравнения
                    string valueWithout = arrayOfArguments[2].Replace("'","");
                    //начинаем читать построчки
                    while (!reader.EndOfStream)
                    {
                        //чтение строчки в файле (в данном случае будет только одно значение)
                        string word = reader.ReadLine();
                        
                        //сравниваем строки
                        if (getCompareStrings(arrayOfArguments[1], valueWithout,
                            word))
                        {
                           bitmap.Set(index, true);
                        }

                        index++;
                    }
                }
                else
                {
                    int arg = int.Parse(arrayOfArguments[2]);
                    //другая реализация, тут сравнение с числами (избегаем множественного парсинга)
                    while (!reader.EndOfStream)
                    {
                        //чтение строчки в файле
                        int word = int.Parse(reader.ReadLine());
                        //если сравнение строк прошло успешно
                        if (getCompareInt(arrayOfArguments[1], arg,
                            word)) //парсим значение для сравнения один раз и больше никогда
                        {
                           bitmap.Set(index, true);
                        }

                        index++;
                    }
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Метод для чтения FactResellerTable (для вывода из таблицы фактов)
        /// </summary>
        /// <param name="bitmap">Пересеченная Bitmap'a</param>
        /// <param name="pathTo">Ссылка на таблицу</param>
        /// <returns>Лист с нужными значениями столбца, согласно RoaringBitmap</returns>
        public static List<string> ReadFactResellerForOutput( RoaringBitMap bitmap, string pathTo)
        {
            List<string> outptutStrings = new List<string>();
            using(FileStream file  = new FileStream($"{Program.dataPath}/{pathTo}.csv", FileMode.Open,
                FileAccess.Read))
            using (StreamReader reader = new StreamReader(file))
            {
                uint index = 1;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (bitmap.Get(index))
                        outptutStrings.Add(line);
                    index++;
                }
            }

            return outptutStrings;
        }

        /// <summary>
        /// Метод для чтения Dim-таблиц (для вывода необходимых элементов из столбцов подтаблиц)
        /// </summary>
        /// <param name="bitMap">Данные по нужным значениям (для поиска ключей в таблице )</param>
        /// <param name="pathTo">Ссылка на таблицу</param>
        /// <returns>Лист значений</returns>
        public static List<string> ReadDimForOutput(RoaringBitMap bitMap, string pathTo)
        {
            List<string> keys = new List<string>();
            string[] fileNames = pathTo.Split('.');
            using(FileStream file = new FileStream($"{Program.dataPath}/FactResellerSales.{FactoryConnections[fileNames[0]]}.csv", FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(file))
            {
                uint index = 1;
                string line  = String.Empty;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (bitMap.Get(index))
                        keys.Add(line);
                    index++;
                }
            }
            //получив ключи - проходимся непосредственно по таблице и собираем необходимые нам значения
            string[] result = new string[keys.Count];
            
            using(FileStream secondfile = new FileStream($"{Program.dataPath}/{fileNames[0]}.csv", FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(secondfile))
            {
                string[] line;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine().Split(new [] {"|"}, StringSplitOptions.RemoveEmptyEntries);
                    if(keys.Contains(line[0]))
                        for(int i = 0; i < keys.Count; i++)
                            if (keys[i].Equals(line[0]))
                                result[i] = line[Tables[fileNames[0]].IndexOf(fileNames[1])];
                }
            }

            return result.ToList();
        }
        
        /// <summary>
        /// Переводчик предикатов, передаваемых пользователем (строка)
        /// </summary>
        /// <param name="comparison">Непосредственно сравнение, которое передал пользователь</param>
        /// <param name="compareWith">Значение с которым необходимо сравнить (задает пользователь)</param>
        /// <param name="value">Сравниваемое значение (берется из таблицы)</param>
        /// <returns>Удовлетворяет ли предикату или нет</returns>
        private static bool getCompareStrings(string comparison, string compareWith, string value)
        {
            switch(comparison)
            {
                case "<>":
                    return !value.Equals(compareWith);
                case "=":
                    return value.Equals(compareWith);
                default:
                    return false;
            }
        }
        
        
        /// <summary>
        /// Переводчик предикатов, передаваемых пользователем (число)
        /// </summary>
        /// <param name="comparison">Непосредственно сравнение, которое передал пользователь</param>
        /// <param name="compareWith">Значение с которым необходимо сравнить (задает пользователь)</param>
        /// <param name="value">Сравниваемое значение (берется из таблицы)</param>
        /// <returns>Удовлетворяет ли предикату или нет</returns>
        private static bool getCompareInt(string comparsion, int compareWith, int value)
        {
            switch(comparsion)
            {
                case "<>":
                    return value != compareWith;
                case "=":
                    return value == compareWith;
                case ">=":
                    return value >= compareWith;
                case "<=":
                    return value <= compareWith;
                case ">":
                    return value > compareWith;
                case "<":
                    return value < compareWith;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Вывод данных в файл теста
        /// </summary>
        /// <param name="outputStrings">Данные для вывода</param>
        /// <param name="testNumber">Номер теста</param>
        public static void OutPut(List<List<string>> outputStrings, string testOutPath)
        {
            using(FileStream file = new FileStream($"{testOutPath}",FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(file))
            {
                foreach (var outputString in outputStrings)
                {
                    writer.WriteLine(String.Join("|", outputString));
                }
            }
        }

        //Далее представлены листы таблиц, содержащие наименования столбцов по порядку (воизбежание необходимости
        //создания класса под каждую таблицу)
        static List<string> _dimResellerTable = new List<string>()
        {
            "ResellerKey",
            "ResellerAlternateKey",
            "Phone",
            "BusinessType",
            "ResellerName",
            "NumberEmployees",
            "OrderFrequency",
            "ProductLine",
            "AddressLine1",
            "BankName",
            "YearOpened"
        };

        static List<string> _dimCurrencyTable = new List<string>()
        {
            "CurrencyKey",
            "CurrencyAlternateKey",
            "CurrencyName"
        };

        static List<string> _dimEmployeeTable = new List<string>()
        {
            "EmployeeKey",
            "FirstName",
            "LastName",
            "Title",
            "BirthDate",
            "LoginID",
            "EmailAddress",
            "Phone",
            "MaritalStatus",
            "Gender",
            "PayFrequency",
            "VacationHours",
            "SickLeaveHours",
            "DepartmentName",
            "StartDate"
        };

        static List<string> _dimSalesTerritoryTable = new List<string>()
        {
            "SalesTerritoryKey",
            "SalesTerritoryAlternateKey",
            "SalesTerritoryRegion",
            "SalesTerritoryCountry",
            "SalesTerritoryGroup"
        };

        private static List<string> _dimDateTable = new List<string>()
        {
            "DateKey",
            "FullDateAlternateKey",
            "DayNumberOfWeek",
            "EnglishDayNameOfWeek",
            "DayNumberOfMonth",
            "DayNumberOfYear",
            "WeekNumberOfYear",
            "EnglishMonthName",
            "MonthNumberOfYear",
            "CalendarQuarter",
            "CalendarYear",
            "CalendarSemester",
            "FiscalQuarter",
            "FiscalYear",
            "FiscalSemester"
        };

        private static List<string> _dimPromotionTable = new List<string>()
        {
            "PromotionKey",
            "PromotionAlternateKey",
            "EnglishPromotionName",
            "EnglishPromotionType",
            "EnglishPromotionCategory",
            "StartDate",
            "EndDate",
            "MinQty"
        };

        private static List<string> _factResellerSalesTable = new List<string>()
        {
            "SalesOrderNumber",
            "SalesOrderLineNumber",
            "ProductKey",
            "OrderDateKey",
            "ResellerKey",
            "EmployeeKey",
            "PromotionKey",
            "CurrencyKey",
            "SalesTerritoryKey",
            "SalesOrderNumber",
            "SalesOrderLineNumber",
            "OrderQuantity",
            "CarrierTrackingNumber",
            "CustomerPONumber"
        };

        private static List<string> _dimProductTable = new List<string>()
        {
            "ProductKey",
            "ProductAlternateKey",
            "EnglishProductName",
            "Color",
            "SafetyStockLevel",
            "ReorderPoint",
            "SizeRange",
            "DaysToManufacture",
            "StartDate"
        };

        public static Dictionary<string, List<string>> Tables = new Dictionary<string, List<string>>()
        {
            ["DimReseller"] = _dimResellerTable,
            ["DimCurrency"] = _dimCurrencyTable,
            ["DimEmployee"] = _dimEmployeeTable,
            ["DimSalesTerritory"] = _dimSalesTerritoryTable,
            ["DimDate"] = _dimDateTable,
            ["DimPromotion"] = _dimPromotionTable,
            ["FactResellerSales"] = _factResellerSalesTable,
            ["DimProduct"] = _dimProductTable
        };

        public static Dictionary<string, string> FactoryConnections = new Dictionary<string, string>()
        {
            ["DimProduct"] = "ProductKey",
            ["DimPromotion"] = "PromotionKey",
            ["DimSalesTerritory"] = "SalesTerritoryKey",
            ["DimCurrency"] = "CurrencyKey",
            ["DimEmployee"] = "EmployeeKey",
            ["DimReseller"] = "ResellerKey",
            ["DimDate"] = "OrderDateKey"
        };
    }
}