#include <iostream>
#include <fstream>
#include <map>
#include <unordered_map>

using namespace std;

//класс для представления одного бакета - контейнер для хранения массива из четырех байт - отпечатков ключей
class Bucket {
private:
    //массив для хранения отпечатков
    int8_t _arrayOfBytes[4];
    //криетрий заполненности массива (для итерации только по существующим элементам)
    int filledPositions = 0;
public:
    //добавление отпечатка в массив
    void add(int8_t x) {
        _arrayOfBytes[filledPositions++] = x;
    }
    //есть ли еще место в массиве
    bool hasEmptyEntries() {
        return filledPositions < 4;
    }
    //получение элемента массива по индексу
    int8_t get(const int& i) {
        return _arrayOfBytes[i];
    }
    //присвоение элементу по конкретному индексу i
    //определенного значения x (фингерпринта)
    void set(const int& i, int8_t x) {
        _arrayOfBytes[i] = x;
    }
    //проверка наличия передаваемого отпечатка в бакете
    bool hasElement(int8_t fingerPrint) {
        for (int i = 0; i < filledPositions; ++i) {
            if (_arrayOfBytes[i] == fingerPrint)
                return true;
        }
        return false;
    }
};
//класс для представления одного кукушкиного фильтра
class Cuckoo {
private:
    //массив бакетов
    Bucket *_arrayOfBuckets;
    //количество групп элементов в фильтре (количество бакетов)
    int m;
public:
    //конструктор для инициализации одного фильтра
    Cuckoo(const int& n) {
        //количество бкетов в фильтре (округляем до верхней степени двойки для корректного ксора)
        m = pow(2, ceil(log(((1 + 0.06) * n))/log(2)));
        //инициализация массива бакетов
        _arrayOfBuckets = new Bucket[m];
    }
    //пустой конструктор для корректной работы словаря
    Cuckoo() = default;

    //вставка в кукушкин филтр
    bool Insert(string& key) {
        //рассчитываем отпечаток
        int8_t f = (int8_t)(hash<string>()(key) % 128);
        //берем хэш от ключа
        auto i1 = hash<string>()(key);
        //расчитываем i2 посредством ксора хэша отпечатка и хэша от ключа
        auto i2 = i1 ^ hash<int8_t>()(f);

        //если хотя бы в одном бакете есть свободное место - заполняем
        if (_arrayOfBuckets[i1 % m].hasEmptyEntries()) {
            _arrayOfBuckets[i1 % m].add(f);
            //прерываем метод
            return true;
        } else if (_arrayOfBuckets[i2 % m].hasEmptyEntries()) {
            _arrayOfBuckets[i2 % m].add(f);
            //прерываем метод - вставка прошла успешно
            return true;
        }
        //выбираем рандомно бакет
        auto i = rand() % 2 == 0 ? i1 : i2;
        //проходим 500 раз по массиву бакетов
        for (int k = 0; k < 500; ++k) {
            //выираем случайную позицию из бакета от 0 до 3
            auto randomPosition = rand() % 4;
            //меняем отпечатки местами
            int8_t nf = _arrayOfBuckets[i % m].get(randomPosition);
            _arrayOfBuckets[i % m].set(randomPosition, f);
            f = nf;
            //получаем номер следующего бакета
            i = i ^ hash<int8_t>()(f);
            //смотрим есть ли в бакете свободное место и заменяем
            if (_arrayOfBuckets[i % m].hasEmptyEntries()) {
                _arrayOfBuckets[i % m].add(f);
                return true;
            }
            //если свободного места нет
        }
        //если мы не вышли из метода - что то идет не так и вставка не удалась
        return false;
    }
    //поиск элемента по ключу в кукушкином фильтре
    bool Lookup(string& key) {
        //рассчитываем отпечаток
        int8_t f = (int8_t)(hash<string>()(key) % 128);
        //берем хэш от ключа
        auto i1 = hash<string>()(key);
        //расчитываем i2 посредством ксора хэша отпечатка и хэша от ключа
        auto i2 = i1 ^ hash<int8_t>()(f);
        //если хотя бы один бакет содержит элемент
        if (_arrayOfBuckets[i1 % m].hasElement(f) or _arrayOfBuckets[i2 % m].hasElement(f))
            return true;
        return false;
    }

};


int main(int argc, char *argv[]) {
    //считываем из файла с тестом
    ifstream in {argv[1]};
    //записываем результат в файл вывода
    ofstream out {argv[2]};
    int n;
    //считанные: команда, пользователь, видео
    string command, user, video;
    //словарь для хранения фильтров пользователей
    unordered_map<string, Cuckoo> dict;
    //пробуем считать начальную строчку
    try {
        in >> command >> n;
    }
    //если не смогли - программа завершается с ошибкой
    catch (exception&) {
        cout << "Error" << endl;
        out << "No" << endl;
        return 1;
    }
    //считывание вводной строчки
    out << "Ok" << endl;
    //считываем до конца файла
    while (!in.eof()) {
        //зануляем аргументы (на соучай если попадется пустая строка в тесте)
        command = user = video = "";
        //считываем строку
        in >> command >> user >> video;
        //если пользователя нет в базе данных (словаре) - добавляем ему отдельный фильтр
        if (!dict.count(user)) {
            dict[user] = Cuckoo(n);
        }
        //если команда - посмотреть - вставляем элемент в кукушкин фильтр
        if (command == "watch") {
            //вставляем и параллельно проверяем все ли успешно
            if (dict[user].Insert(video)) {
                out << "Ok" << endl;
            } else {
                //если произошла ошибка вставки
                out << "No" << endl;
            }
            //если команда - проверить смотрел ли пользователь видео
        } else if (command == "check") {
            //ищем в кукушкином фильтре данное видео
            if (dict[user].Lookup(video)) {
                out << "Probably" << endl;
            } else {
                out << "No" << endl;
            }
        }
    }
}
