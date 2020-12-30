using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;


namespace Graham
{
  internal class Program
  {
    //поле для хранения стэка
    private static Stack stack;
      //поле для хранения списка точек для последующей обработки в процессе алгоритма Грэхэма
    private static List<Point> points;
      //изначальный список точек для вывода в WKT
    private static List<Point> beginPosition;

    public static void Main(string[] args)
    {
      
      {
        Input4Graham(args[2]);
      
        ChooseGrahamType(args[0]);
      
        ChooseOutputType(args[1], args[3]);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"-> TEST: {args[2]} <-> SUCCESS <-> OUTPUT:{args[3]}");
        Console.ResetColor();

      }
      /*catch (Exception e)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"-> TEST: {args[2]} <-> FAILED" + "\n" + "INFO: "+e.Message);
        Console.ResetColor();
      }*/
     
    
    }
    
    //регион, содержащий методы для первичной обработки запроса пользователя
    #region ControlStation

    /// <summary>
    /// Метод осуществляет обработку выбора определенного типа сортировки алгоримта Грэхэма
    /// (по часовой или против часовой стрелки)
    /// </summary>
    /// <param name="algoType">Тип (либо cc, либо cw)</param>
    private static void ChooseGrahamType(string algoType)
    {
      switch (algoType)
      {
        case ("cw"):
        {
          Graham(points);
          break;
        }
        case ("cc"):
        {
          CounterGraham(points);
          break;
        }
      }
    }

    /// <summary>
    /// Обработка точек для последующего испольщзования в алгоритме (парсинг и сортирвка)
    /// </summary>
    /// <param name="path"></param>
    private static void Input4Graham(string path)
    {
      points = FileReader(path);
      beginPosition = FileReader(path);
      
      points.Sort((point1, point2) =>
      {
        if (point1.Y < point2.Y)
          return -1;
        else if (point1.Y > point2.Y)
          return 1;
        else if (point1.X < point2.X)
          return -1;
        return 1;
      });
      
      stack = new Stack(points.Count);
      stack.Push(points[0]);
    }

    /// <summary>
    /// Выбор типа вывода результатов алгоритма (Well-Known-Text/Plain)
    /// </summary>
    /// <param name="outputType">тип вывода</param>
    /// <param name="outputPath">ссылка для вывода</param>
    private static void ChooseOutputType(string outputType, string outputPath)
    {
      switch (outputType)
      {
        case ("plain"):
        {
          FileWriter(outputPath, stack.ToString());
          break;
        }
        case ("wkt"):
        {
          FileWriter(outputPath, stack.ToString(beginPosition));
          break;
        }
      }
    }
    #endregion
    
    //регион, содержащий методы для обеспечения работы алгоритма Грэхэма ПРОТИВ ЧАСОВОЙ СТРЕЛКИ
    #region CounterClockWiseGrahamMethods

    /// <summary>
    /// Функция определяет, с какой стороны от вектора лежит данная точка
    /// </summary>
    /// <param name="A">Начало вектора</param>
    /// <param name="B">конец вектора</param>
    /// <param name="C">Точка положение которой необходимо найти</param>
    /// <returns>положительное знгачение - слева, отрицательное значение - справа</returns>
    private static int Side(Point A, Point B, Point C)
      => (B.X - A.X) * (C.Y - B.Y) - (B.Y - A.Y) * (C.X - B.X);
    
    /// <summary>
    /// Вычисляем полярный угол между вектором
    /// </summary>
    /// <param name="A">Точка А вектора</param>
    /// <param name="B">Точка В вектора</param>
    /// <returns></returns>
    private static double Polar(Point A, Point B)
    {
      double result = Math.Atan2(A.Y - B.Y, A.X - B.X);
      if (result < 0)
        return result + 2 * Math.PI;
      return result;
    }

    private static double LengthTillB(Point A, Point B)
      =>  (Math.Pow(A.X - B.X, 2) + Math.Pow(A.Y - B.Y, 2));

    /// <summary>
    /// Непосредственно сам алгоритм Грэхэма против часовой стрелки
    /// </summary>
    /// <param name="points">точки-деревья (не включая крайней нижней левой)</param>
    private static void CounterGraham(List<Point> points)
    {
      points = points
        .Where((x) => x != points[0])
        .OrderBy(point => Polar(point, points[0])).ThenBy(x => LengthTillB(x, points[0])).ToList();
       
       
      int length = points.Count;
      List<Point> rightList = new List<Point>();

      for (int i = 0; i < length; i++)
      {
        if (i + 1 < length)
        {
          if(Polar(points[i], stack.Top()) != Polar(points[i+1], stack.Top()))
          rightList.Add(points[i]);
        }
        else
        {
          rightList.Add(points[i]);
        }
      }
      points = rightList;
      
      stack.Push(points[0]);


      for (int i = 1; i < points.Count; i++)
      {
        while (stack.Size> 1 && (Side(stack.NextToTop(),stack.Top(),  points[i]) <= 0))
          stack.Pop();
        stack.Push(points[i]);
      }
    }
    #endregion
    
    //регион, содержащий методы для обеспечения работы алгоритма Грэхэма ПО ЧАСОВОЙ СТРЕЛКЕ
    #region ClockWideGrahamMethods

    /// <summary>
    /// Непосредственно реализация алгоритма Грэхэма с проходом по часовой стрелке
    /// </summary>
    /// <param name="points">Точки считанные из файла, кроме нижней левой точки (по условию)</param>
    private static void Graham(List<Point> points)
    {
      points = points.Where((x) => x != points[0]).OrderByDescending(point => Polar(point, points[0])).ThenBy(x => LengthTillB(x, points[0])).ToList();
      
      int length = points.Count;
      List<Point> rightList = new List<Point>();
      
      //очищаем точки лежащие на одной прямой относительно зафиксированной точки
      for (int i = 0; i < length; i++)
      {
        if (i + 1 < length)
        {
          if(Polar(points[i], stack.Top()) != Polar(points[i+1], stack.Top()))
            rightList.Add(points[i]);
        }
        else
        {
          rightList.Add(points[i]);
        }
      }

      points = rightList;
      stack.Push(points[0]);
      
      for (int i = 1; i < points.Count; i++)
      {
        while (stack.Size > 1 && Side(stack.NextToTop(), stack.Top(), points[i]) >= 0)
          stack.Pop();
        stack.Push(points[i]);
      }
    }
    #endregion
    
    //регион, содержащий методы для работы с файлами (считывание файла с входными данными, вывод данных в отдельный файл)
    #region WorkWithFile
    /// <summary>
    /// Вывод результатов алгоритма в файл
    /// </summary>
    /// <param name="path">Ссылка для вывода информации</param>
    /// <param name="content">Результат работы алгоритма в виде строки</param>
    private static void FileWriter(string path, string content)
    {
      try
      {
        using(FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (StreamWriter writer = new StreamWriter(stream))
        {
          writer.WriteLine(content);
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
    /// <summary>
    /// Чтение файла (теста)
    /// </summary>
    /// <param name="path">Ссылка на тест</param>
    /// <returns>Список считанных точек</returns>
    /// <exception cref="ArgumentException">Если некорректный тест (в начале не указано количество точек) -
    /// выбрасывается данное исключение</exception>
    
    private static List<Point> FileReader(string path)
    {
      try
      {
        List<string> info = new List<string>();
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (StreamReader reader = new StreamReader(stream))
        {
          //чтение файла
          while (!reader.EndOfStream)
            info.Add(reader.ReadLine());
        }

        //парсинг количества точек (первая строка)
        int count;
        if(!int.TryParse(info[0], out count) || string.IsNullOrEmpty(info[0]))
          throw new ArgumentException("В первой строке не число!");

        //парсинг точек
        List<string> line;
        List<Point> points = new List<Point>();
        for (int i = 1; i < count+1; i++)
        {
          line = info[i].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
          points.Add(new Point(int.Parse(line[0]), int.Parse(line[1])));
        }
        
        return points;
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

    #endregion
  }


  /// <summary>
  /// класс, отвечающий за координаты конкретной точки
  /// </summary>
  class Point
  {
    //абсцисса
    private int x;
    //ордината
    private int y;
    
    //свойство для доступа к абсциссе точки
    public int X
    {
      get => x;
    }
    //свойство для доступа к ординате точки
    public int Y
    {
      get => y;
    }

    public Point(int x, int y)
    {
      this.x = x;
      this.y = y;
    }

    public override string ToString()
      => $"{x} {y}";
  }

  /// <summary>
  /// Класс представляющий собой собственную реализацию стэка
  /// </summary>
  class Stack
  {
    //максимальный размер стэка
    private const int maxSize = 1000;
    //стэк точек
    private Point[] stack;
    //количество точек в стэке
    private int count;

    //Является ли стэк пустым
    public bool IsEmpty
    {
      get { return count == 0; }
    }
    //Свойство для досупа к текущему размеру стэка
    public int Size
    {
      get { return count; }
    }
    /// <summary>
    /// Метод для добавления новго элемента в стэк
    /// </summary>
    /// <param name="point">Точку, которую необходимо добавить в стэк</param>
    /// <exception cref="InvalidOperationException">Если стэк переполнен - выкидывается исключение,
    /// уведомляющее об ошибке</exception>
    public void Push(Point point)
    {
      if (count == stack.Length) 
        throw new InvalidOperationException("Переполнение стека");
      stack[count++] = point;
    }
    
    
    /// <summary>
    /// Конструкор для создания нового стэка
    /// </summary>
    /// <param name="numberOfPoints">Количество точек в нашем стэке (фиксированное число)</param>
    /// <exception cref="ArgumentException">Если число превышает наибольшее допустимое по заданию константное занчение -
    /// выкидывается соответствующее исключение</exception>
    public Stack(int numberOfPoints)
    {
      if(numberOfPoints <= maxSize)
       stack = new Point[numberOfPoints];
      else
        throw new ArgumentException("Слишком большой размер стэка!");
    } 
    /// <summary>
    /// Удаление элемента из стэка
    /// </summary>
    /// <returns>Возвращает удаленный элемент с вершины стэка</returns>
    /// <exception cref="InvalidOperationException">Если стэк пуст - выкидывается исключение, уведомляющее об этой операции</exception>
    public Point Pop()
    {
      if (IsEmpty)
        throw new InvalidOperationException("Стек пустой");
      Point point = stack[--count];
      stack[count] = null;
      return point;
    }

    /// <summary>
    /// Метод возвращающий последний элемент стэка (последний добавленный)
    /// </summary>
    /// <returns>Верхний (последний элемент) стэка</returns>
    public Point Top()
      => stack[count - 1];

    /// <summary>
    /// Метод, возвращащий второй сверху элемент нашего стэка
    /// </summary>
    /// <returns>Второй сверху элемент стэка</returns>
    public Point NextToTop()
      => stack[count - 2];

    /// <summary>
    /// Метод для вывода резульататов алгоритма в виде Plain
    /// </summary>
    /// <returns>Строка, содержащая результат алгоритма Грэхэма в соответствии с типом вывода Plain</returns>
    public override string ToString()
    {
      StringBuilder newLine = new StringBuilder();
      if (Size < 3)
        return "0";
      for (int i = 0; i < Size; i++)
        newLine.Append(stack[i] + "\n");
      return Size + "\n" + newLine;
    }

    /// <summary>
    /// Метод для вывода резульататов алгоритма в виде WKT
    /// </summary>
    /// <param name="beginPoints">Изначальный список точек (считанный из файла, как есть)</param>
    /// <returns>Строка, содержащая результат алгоритма Грэхэма в соответствии с типом выода WKT</returns>
    public  string ToString(List<Point> beginPoints)
    {
      StringBuilder output = new StringBuilder("MULTIPOINT (");
      List<string> multipoint = new List<string>();
      for (int i = 0; i < beginPoints.Count; i++)
      {
        multipoint.Add($"({beginPoints[i].X} {beginPoints[i].Y})");
      }

      if (Size < 3)
        return output.Append($"{String.Join(", ", multipoint)})" + "\n" + "NOT A POLYGON").ToString();
      output.Append($"{String.Join(", ", multipoint)})" + "\n" + "POLYGON ((");
      
      multipoint = new List<string>();
      for (int i = 0; i < Size; i++)
        multipoint.Add($"{stack[i].X} {stack[i].Y}");
      
      output.Append($"{String.Join(", ", multipoint)}, {stack[0].X} {stack[0].Y}))");

      return output.ToString();
    }
    
  }
}