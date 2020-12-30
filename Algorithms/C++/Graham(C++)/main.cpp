#include <iostream>
#include <string>
#include <vector>
#include <cmath>
#include <algorithm>
#include <fstream>

using namespace std;

//класс для описания точки на плоскости с координатами X и Y соответственно
class Point
{

public:
    //дефолтный конструктор
    Point()
    = default;
    //конструктор для конкретной точки с конкретными координатами X и Y
    Point(int x, int y)
    {
        this->x = x;
        this->y = y;
    }
    //метода для доступа к абсциссе точки
     int X() const
    {return x;}
    //мтетод для доступа к ординате точки
    int Y() const
    {return y;}
    //метод для вывода информации о точке
    string ToString() const
    {return  to_string(X()) + " " + to_string(Y());}

private:
    //значения по умолчанию для непроинициализированной точки
    int x = 0,y = 0;
};

//массив точек из файла (так легче работать, чем постоянно передавать в методы)
vector<Point> points;
//исключительно для вывода изначального списка точек
vector<Point> beginPoints;
//самостоятельно реализованная структура данных - стэк
class Stack
{
public:
    //дефолтный конструктор
    Stack() = default;
    //конструктор с определенным фиксированным значением стэка
    explicit Stack(int numberOfPoints)
    {
        if(numberOfPoints <= maxSize){
            stack = vector<Point>(numberOfPoints);
        }
        else
            throw "Слишком большой размер стэка!";
    }
    //Опустел ли стэк
    bool IsEmpty() const
    { return count == 0;}
    //текущий размер стэка
    int Size() const
    {return count;}
    //добавление элемента в стэк
    void Push(Point point)
    {
        if(Size() == stack.size())
            throw "Переполнение стека!";
        stack[count++] = point;
    }
    //удаление верхнего элемента из стэка
    Point Pop()
    {
        if(IsEmpty())
            throw "Стек пуст!";
        Point point = stack[--count];
        stack.pop_back();
        return point;
    }
    //получение верхнего элемента стэка
    Point Top()
    {
        return stack[count-1];
    }
    //получение второго сверху элемента стэка
    Point NextToTop()
    {
        return stack[count-2];
    }
    //вывод данных о стэке точек в формате plain
    string ToString()
    {
        string line;
        if(Size() < 3)
            return "0";
        for(int i = 0; i < Size(); i++)
            line.append(stack[i].ToString() + "\n");
        return to_string(Size()) + "\n" + line;
    }
    //вывод данных о стэке точек в формате wkt
    string ToString(vector<Point> originalArray)
    {

        string output = "MULTIPOINT (";
        for(int i = 0; i < originalArray.size(); i++)
            if(i != originalArray.size() - 1)
                output.append("(" + originalArray[i].ToString() + "), ");
            else
                output.append("(" + originalArray[i].ToString() + "))");

        if(Size() < 3)
            return output.append("\nNOT A POLYGON");


        output.append("\nPOLYGON ((");
        for(int i = 0; i < count; i++)
            if(i != count - 1)
                output.append( stack[i].ToString() + ", ");
            else
                output.append(stack[i].ToString() +", " +stack[0].ToString() +"))");

        return output;
    }
private:

    const static int maxSize = 1000;

    vector<Point> stack;

    int count = 0;

};


Stack myStack;

//готовое считывание точек из файла
void FileReader(const string& path)
{
    int numberOfPoints = 0;
    int x = 0, y = 0;

    ifstream in (path); // окрываем файл для чтения
    if (in.is_open())
    {
       in >> numberOfPoints;
       for(int i = 0; i < numberOfPoints; i++)
       {
           in >> x >> y;
           points.emplace_back(Point(x,y));
       }
    }
    in.close();// закрываем файл
}
//обработчик для считывания из файла (промежуточный метод)
void Input4Graham(const string& path)
{
    //считывание файла
    FileReader(path);
    beginPoints = points;
    //поиск левого нижнего элемента
    sort(points.begin(), points.end(), [](Point point1, Point point2)
    {
        if(point1.Y() != point2.Y())
            return point1.Y() < point2.Y();
        else return point1.X() < point2.X();
    });
    //добавляем найденный элемент в стэк
    myStack =  Stack (points.size());
   myStack.Push(points[0]);
}

//определяем по какую сторону от вектора лежит точка
int Side(Point A, Point B, Point C)
{
    return (B.X() - A.X())*(C.Y() - B.Y()) - (B.Y() - A.Y())*(C.X() - B.X());
}
//расстояние от точки до точки
double LengthTillB(Point A, Point B)
{
    return (pow(A.X() - B.X(), 2) + pow(A.Y() - B.Y(), 2));
}
//нахождение полярного угла
double Polar(Point A, Point B)
{
    double result = atan2(A.Y()-B.Y(), A.X() - B.X());
    if(result < 0)
        return result + 2 * M_PI;
    return result;
}
//первая точка (для сортировки)
Point firstPoint;


void CounterGraham()
{
    firstPoint = Point(points[0].X(), points[0].Y());
    //сортируем в начале по полярному углу, затем по расстоянию от нижней левой (контрольной) точки
    sort(points.begin()+1, points.end(), [](Point point1, Point point2) {
        if(Polar(point1, firstPoint) > Polar(point2, firstPoint))
            return false;
        else if(Polar(point1, firstPoint) == Polar(point2, firstPoint))
            if(LengthTillB(point1, firstPoint) >= LengthTillB(point2, firstPoint))
                return false;
            return true;
    });

    int length  = points.size();
    vector<Point> rightList;
    //удаляем точки на одной прямой от контрольной точки
    for(int i = 1; i < length; i++)
    {
        if(i+1 < length)
        {
            if(Polar(points[i], myStack.Top()) != Polar(points[i + 1], myStack.Top()))
                rightList.push_back(points[i]);
        }
        else
        {
            rightList.push_back(points[i]);
        }
    }

    points = rightList;
    myStack.Push(points[0]);
    //непосредственно алгоритм Грэхэма в действии
    for(int i = 1; i < points.size(); i++)
    {
        while(myStack.Size() > 1 && (Side(myStack.NextToTop(), myStack.Top(), points[i]) <= 0))
            myStack.Pop();
        myStack.Push(points[i]);
    }

}

//алгоритм Грэхэма только по часовой стрелке (аналогично предыдущему методу)
void Graham()
{
    firstPoint = Point(points[0].X(), points[0].Y());
    sort(points.begin()+1, points.end(), [](Point point1, Point point2) {
        if(Polar(point1, firstPoint) > Polar(point2, firstPoint))
            return true;
        else if(Polar(point1, firstPoint) == Polar(point2, firstPoint))
            if(LengthTillB(point1, firstPoint) <= LengthTillB(point2, firstPoint))
                return true;
        return false;
    });

    int length  = points.size();
    vector<Point> rightList;
    for(int i = 1; i < length; i++)
    {
        if(i+1 < length)
        {
            if(Polar(points[i], myStack.Top()) != Polar(points[i + 1], myStack.Top()))
                rightList.push_back(points[i]);
        }
        else
        {
            rightList.push_back(points[i]);
        }
    }

    points = rightList;
    myStack.Push(points[0]);

    for(int i = 1; i < points.size(); i++)
    {
        while(myStack.Size() > 1 && (Side(myStack.NextToTop(), myStack.Top(), points[i]) >= 0))
            myStack.Pop();
        myStack.Push(points[i]);
    }

}
//промежуточный метод для выбора типа обхода (слева или справа) - по часовой или против
void ChooseGrahamType(const string& algoType)
{
    if(algoType == "cw")
    {
        Graham();
    }
    else if (algoType == "cc")
    {
        CounterGraham();
    }

}
//запись результатов обратно в файл
void FileWriter(const string& path, const string& text)
{
    ofstream outF (path);
    if(outF.is_open())
    {
        outF << text;
    }
    outF.close();
}
//промежуточный метод для выбора типа вывода
void ChooseOutputType(const string& type, const string& path)
{
    if(type == "wkt")
    {
        FileWriter(path, myStack.ToString(beginPoints));
    }
    else if (type == "plain")
    {
        FileWriter(path, myStack.ToString());
    }
}

//непосредственно головной центр алгоритма (вызываются промежуточные за ними сам алгоритм и затем вывод)
int main(int argc, char *argv[]) {
    Input4Graham(argv[3]);
    ChooseGrahamType(argv[1]);
    ChooseOutputType(argv[2], argv[4]);

//    Input4Graham("/Users/toomanyquestions/Desktop/Coursera/C++/Graham/test1.txt");
//    ChooseGrahamType("cc");
//    ChooseOutputType("plain", "1.txt");
 return 0;
}
