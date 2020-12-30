#include <iostream>
#include <vector>
#include <optional>
#include<algorithm>
#include <fstream>
#include <map>

using namespace std;

//методы для сериализации и десериализации
namespace MyBinSerialization {
    template<class T>
    void writeVector(std::ofstream &output, const std::vector<T> &vector) {
        // Записываем размер вектора
        int size = vector.size();
        output.write(reinterpret_cast<const char *>(&size), sizeof(size));

        // Записываем элементы вектора
        for (auto &item : vector) {
            output.write(reinterpret_cast<const char *>(&item), sizeof(T));
        }
    }

    template<class T>
    void writeVariable(std::ofstream &output, const T &variable) {
        output.write(reinterpret_cast<const char *>(&variable), sizeof(T));
    }

    template<class T>
    void readVariable(std::ifstream &input, T &variable) {
        input.read(reinterpret_cast<char *>(&variable), sizeof(T));
    }

    template<class T>
    void readVector(std::ifstream &input, vector<T> &vector) {
        // Читаем размер вектора
        int size = 0;
        input.read(reinterpret_cast<char *>(&size), sizeof(size));

        // Читаем элементы вектора
        T item;
        vector.clear();
        vector.reserve(size);
        for (int i = 0; i < size; ++i) {
            input.read(reinterpret_cast<char *>(&item), sizeof(T));
            vector.push_back(item);
        }
    }
}

//глобальное ограничение
int T;
//количество нодов (для вычисления имен файлов)
int nodeNumber = 0;

string pathToFolder;

class Node {
public:

    Node(int number) {
        //название текущего нода
        fileName = "Node<" + to_string(number) + ">";
        //количсетво ключей (изначально)
        cntKeys = 0;
        //инциализация ключей и значений
        keys = vector<int>(2 * T - 1);
        values = vector<int>(2 * T - 1);
        //инициализация массива детей (имена файлов)
        children = vector<string>(2 * T);
        //является ли текущий нод листом
        isLeaf = true;
    }

    Node() {
        //количсетво ключей (изначально)
        cntKeys = 0;
        //инциализация ключей и значений
        keys = vector<int>(2 * T - 1);
        values = vector<int>(2 * T - 1);
        //инициализация массива детей (имена файлов)
        children = vector<string>(2 * T);
        //является ли текущий нод листом
        isLeaf = true;
    }

    //количество ключей
    size_t cntKeys;
    //хранит ключи
    vector<int> keys;
    //хранит, является ли узел листом
    bool isLeaf;
    //хранит ссылки на потомков
    vector<string> children;
    //хранит значения
    vector<int> values;
    //имя файла, где хранятся данные для этого узла
    string fileName;

    //Считывает данные из файла, соответствующего данному узлу
    Node &read(const string fileChildName) {
        ifstream in{pathToFolder +"/"+ fileChildName + ".bin", ios_base::binary};

        MyBinSerialization::readVariable(in, cntKeys);
        MyBinSerialization::readVariable(in, isLeaf);

        MyBinSerialization::readVector(in, keys);
        MyBinSerialization::readVector(in, values);
        MyBinSerialization::readVector(in, children);
        fileName = fileChildName;
        return *this;
    }

    //Записывает данные в файл, соответсвующий данному узлу
    void write() {
        ofstream out{pathToFolder + "/" + fileName + ".bin", ios_base::binary};

        MyBinSerialization::writeVariable(out, cntKeys);
        MyBinSerialization::writeVariable(out, isLeaf);

        MyBinSerialization::writeVector(out, keys);
        MyBinSerialization::writeVector(out, values);
        MyBinSerialization::writeVector(out, children);

    }

    static void treeSplitChild(Node &x, int i, Node &y) {
        //x - родитель (неполный)
        //y - дочерний узел, который необходимо разделить
        //новый нод (левый)
        Node z = Node(nodeNumber++);
        //если y - был листом, то и новый узел будет листом
        z.isLeaf = y.isLeaf;
        //минимальное количество ключей
        z.cntKeys = T - 1;
        //не берем T-1 элемент - его вставляем в родителя
        //вторую половину дочернего узла копируем в новый узел
        for (int j = 0; j < T - 1; ++j) {
            z.keys[j] = y.keys[j + T];
            //переносим значения вместе с ключами
            z.values[j] = y.values[j + T];
        }
        //если дочерний узел не является листом
        if (!y.isLeaf) {
            //вторую половину детей изначального узла переносим в новый узел
            for (auto j = 0; j < T; ++j) {
                z.children[j] = y.children[j + T];
            }
        }
        //обновляем количество ключей в изначальном узле
        y.cntKeys = T - 1;
        //сдвигаем все элементы до i включительно, чтобы осводить место для ключа
        for (int j = x.cntKeys; j >= i; --j) {
            x.children[j + 1] = x.children[j];
        }
        x.children[i + 1] = z.fileName;
        //также сдвигаем ключи и на i-ое место вставляем наше медианное значение
        for (int j = x.cntKeys - 1; j >= i; --j) {
            x.keys[j + 1] = x.keys[j];
            x.values[j + 1] = x.values[j];
        }
        x.keys[i] = y.keys[T - 1];
        x.values[i] = y.values[T - 1];

        //увеличиваем каунтер ключей
        x.cntKeys++;

        y.write();
        x.write();
        z.write();
    }

};

class BTree {
private:
    //вставка элемента в бДерево
    static bool treeInsertNonFull(Node &node, int k, int value) {
        {
            //если такой ключ уже есть - возвращаем false
            auto m = find(node.keys.begin(), node.keys.begin() + node.cntKeys, k);
            if (m != (node.keys.begin() + node.cntKeys))
                return false;
        }
        //записываем индекс максимального элемента
        int i = node.cntKeys - 1;
        //если текущий нод является листом
        if (node.isLeaf) {

            //ищем индекс ключа меньше чем наш (он не может быть равен) для вставки
            while (i >= 0 && k < node.keys[i]) {
                //параллельно сдвигаем наши массивы ключей/значений
                node.keys[i + 1] = node.keys[i];
                node.values[i + 1] = node.values[i];
                i--;
            }
            //вставляем ключ/значение
            node.keys[i + 1] = k;
            node.values[i + 1] = value;
            //увеличиваем количество ключей
            node.cntKeys++;
            //записываем наш нод
            node.write();
            return true;
        } else
            //если не лист
        {
            //опять ищем индекс ключа который будет меньше нашего (первого)
            while (i >= 0 && k < node.keys[i]) {
                i--;
            }
            //берем от него следующий
            i++;
            //создаем новый нод для чтения
            auto c = Node();
            //читаем ЛЕВОГО РЕБЕНКА i+1 ЭЛЕМЕНТА
            c.read(node.children[i]);
            //если в нем ключей - максимум
            if (c.cntKeys == 2 * T - 1) {
                //разбиваем данного ребенка
                Node::treeSplitChild(node, i, c);
                //если ребенок полный - мы разбиваем его и соответственно вставляем медиану в изначальный нод
                //однако проверяем, если наш ключ больше - обращаемся к левому ребенку i+1 элемента
                //если нет - все к тому же i-ому элементу
                if (k > node.keys[i]) {
                    i++;
                    c.read(node.children[i]);
                }
            }
            //рекурскивно вызываем инсерт, чтобы вставить в ребенка
            return treeInsertNonFull(c, k, value);
        }
    }

    static optional<pair<Node, int>> node_search(Node &node, int k) {
        int i = 0;
        //идем по ключам пока не найдем ключ МЕНЬШИЙ ЛИБО РАВНЫЙ ДАННОМУ
        while (i < node.cntKeys && k > node.keys[i])
            i++;
        //если ключи равны - возвращаем пару <Узел - ключ>
        if (i < node.cntKeys && k == node.keys[i])
            return pair<Node, int>(node, i);
        //если является листом - то возвращаем пустую пару
        if (node.isLeaf)
            return {};
        else {
            //если ключ болше нашего - мы спускаемся на узел ниже и левее (потому что у нас элементов дочерних
            //(i+1)
            auto anotherOne = Node();
            anotherOne.read(node.children[i]);
            return node_search(anotherOne, k);
        }
    }

    //поиск индекса элемента или ребенка данного нода, в который необходимо спуститься для дальнейшего поиска
    int findIndex(int k, Node &node) {
        int idx = 0;
        //ищем минимальный ключ, что будет больше нашего переданного ключа
        //либо получим индекс самого элемента либо индекс большего ключа (то есть индекс ребенка, куда необходимо спуститься
        //для дальнейшего поиска нашего ключа)
        while (idx < node.cntKeys && node.keys[idx] < k)
            ++idx;
        return idx;
    }

    //метод для удаления элемента из листа
    void leafRemove(Node &node, int index) {
        for (int i = index + 1; i < node.cntKeys; ++i) {
            //сдвигаем значения и ключи, тем самым перезаписывая удаленный индекс
            node.keys[i - 1] = node.keys[i];
            node.values[i - 1] = node.values[i];
        }
        //уменьшаем количество ключей
        node.cntKeys--;
        //записываем результат в бинарный файл (обновление)
        node.write();
    }

    //метод для удаления элемента из нода бдерева, имеющего зависимости (детей)
    //основной смысл - поменять местами данный ключ с ближайшим левым или правым (который находится уже в листе и применить
    //стандартный алгоритм удаления из листа описанный выше)
    void removeFromNode(Node &node, int index) {
        //сохраняем значения ключа который будем перемещать в лист
        int k = node.keys[index];
        //сохраняем привязанное к ключу значение
        int value = node.values[index];
        //локальная переменная для хранения ребенка с ближайшим значением
        auto child = Node();
        //если в левом ребенке достаточно значеий для выполнения операции удаления - выбиарем значение оттуда
        if (child.read(node.children[index]).cntKeys >= T) {
            //спускаемся в самого правого наследника левого ребенка
            getClosestLeft(child);
            //происходит замена ключей и значений
            swap(node.keys[index], child.keys[child.cntKeys - 1]);
            swap(node.values[index], child.values[child.cntKeys - 1]);
            //альтернативный код для swap
//            node.keys[index] = pred;
//            node.values[index] = child.values[child.cntKeys - 1];
//
//            child.keys[child.cntKeys-1] = k;
//            child.values[child.cntKeys-1] = value;
            //записываем изменения (успешно поменяли местами значения)
            node.write();
            child.write();
            //рекурсивно вызывается удаление, рекурсия продолжается попутно перестаривая дерево
            deleteFromTree(k, node.read(node.children[index]));
        }
            //если в правом ребенке достаточно элементов для проведения операции (>t-1) то обращаемся к нему
        else if (child.read(node.children[index + 1]).cntKeys >= T) {
            //спускаемся в самого левого наследника правого ребенка, чтобы найти минимальное значение ключа превосходящее наше
            //(которое необходимо удалить)
            getClosestRight(child);
            //меняем значения местами
            swap(node.keys[index], child.keys[0]);
            swap(node.values[index], child.values[0]);
//            node.keys[index] = succ;
//            node.values[index] = child.values[0];
//
//            child.keys[0] = k;
//            child.values[0] = value;
            //фиксируем изменения в ноде и листе посредством записи в файл
            node.write();
            child.write();
            //рекурсивно далее идем по нашему дереву попутно перестраивая ноды (если где то элементов оказывается меньше t-1)
            deleteFromTree(k, node.read(node.children[index + 1]));
        } else {
            //если ни правый ни левый ребенок не могут быть использованы для операции - совмещаем их и рекурсивно выполняем
            //операцию удаления (еще раз идем по дереву, но на этот раз можем совершить операцию, т.к. ключей будет точно (>=t-1)
            merge(index, node);
            deleteFromTree(k, node);
        }
    }

    //метод для свомещения двух детей в одного
    void merge(int index, Node &node) {
        //локальные переменные для работы с левым и правым ребенком данного родителя
        auto child = Node();
        auto sibling = Node();

        //читаем детей
        //считываем из файла левого ребенка
        child.read(node.children[index]);
        //считываем из файла правого ребенка
        sibling.read(node.children[index + 1]);

        //оба ребенка точно имеют t-1 элемент (минимум для любого элемента кроме корня)
        //перемещаем медиану из родителя в ребенка соответственно
        child.keys[T - 1] = node.keys[index];
        child.values[T - 1] = node.values[index];

        //перемещаем детей из правого ребенка в левый (сохраняем зависимости при объединении)
        if (!child.isLeaf) {
            for (int i = 0; i <= sibling.cntKeys; ++i)
                child.children[i + T] = sibling.children[i];
        }
        //перемещаем ключи и привязанные к ним значения из правого ребенка в левого
        for (int i = 0; i < sibling.cntKeys; ++i) {
            child.keys[i + T] = sibling.keys[i];
            child.values[i + T] = sibling.values[i];
        }

        //так как мы забрали медиану из родителя - необходимо сдвинуть все значения в родителе на один
        for (int i = index + 1; i < node.cntKeys; ++i) {
            node.keys[i - 1] = node.keys[i];
            node.values[i - 1] = node.values[i];
        }
        //оставляем левого ребенка, а правого удаляем сдвигая на него index+2 и так далее
        //проще говоря удаляем правого ребенка из зависимостей родителя
        for (int i = index + 2; i <= node.cntKeys; ++i)
            node.children[i - 1] = node.children[i];
        //обновляем количество ключей в левом ребенке - теперь это количество ключей в правом + количество исходных ключей + медина из родителей
        child.cntKeys += sibling.cntKeys + 1;
        //так как мы забрали медиану - уменьшаем количество ключей в родителе
        node.cntKeys--;

        //особый случай - если мы забрали медиану и она была последним элементом в корне (больше там ключей/элементов не осталось)
        if (node.cntKeys == 0) {
            //обновляем название файла корня нашего дерева на замержденного ребенка
            root.fileName = child.fileName;
            //фиксируем изменения в бинарном файле
            child.write();
        } else {
            //иначе просто фиксируем наши изменения в родителе и в правом ребенке (левый ребенок удален и фиксировать изменения в нем
            //нет необходимости
            child.write();
            node.write();
        }
    }

    //находим минимальный элемент значение (ключа) которого больше граничного
    void getClosestRight(Node &child) {
        //спускаемся в самого левого наследника правого ребенка(первый ключ в нем будет самым ближайшим по значению к границе
        //и по совместительству минимальный)
        while (!child.isLeaf)
            child.read(child.children[0]);
        //return child.keys[0];
    }

    //находим максимальный элемент значение (ключа) которого меньше граничного
    void getClosestLeft(Node &child) {
        //спускаемся в самого правого наследника левого ребенка (самый последний ключ в нем будет самым ближайшим по значению к
        //границе и по совместительству максимальным)
        while (!child.isLeaf)
            //всегда помним что количество детей = количество ключей + 1, следовательно крайний ребенок будет под индексом
            //количества ключей
            child.read(child.children[child.cntKeys]);
        // return child.keys[child.cntKeys - 1];
        //сам ключ будет самым крайним в массиве
    }

    //осуществление переноса одного элемента из левого ребенка в правый (из предыдущего ребенка)
    //левый ребенок -> разделитель родителя -> первое место в требуемом ребенке
    void getFromPrevious(int index, Node &node) {
        //ребенок в которого нам необходимо спуститься в течении нашей рекурсии, но в нем недостаточно элементов (<t-1)
        Node child = Node();
        //его левый сосед (предыдущий ребенок)
        Node sibling = Node();
        //производим считывание:
        //считываем ребенка (в который необходимо поместить еще один элемент)
        child.read(node.children[index]);
        //предыдущий ребенок
        sibling.read(node.children[index - 1]);
        //там гарантированно есть место (до вызова метода мы это проверили)
        // - мы сдвигаем ключи и их значения на одну позицию вперед,
        // чтобы поместить на самую первую позицию новый ключ
        //разделитель из основного нода
        for (int i = child.cntKeys - 1; i >= 0; --i) {
            child.keys[i + 1] = child.keys[i];
            child.values[i + 1] = child.values[i];
        }

        //если имеются дети - их также свдигаем на одну позицию, т.к. с новым элементом появляется новая ветка потомков
        if (!child.isLeaf) {
            for (int i = child.cntKeys; i >= 0; --i)
                child.children[i + 1] = child.children[i];
            child.children[0] = sibling.children[sibling.cntKeys];
        }

        //перемещаем граничное значение из родителя в первый ключ в ребенке
        child.keys[0] = node.keys[index - 1];
        child.values[0] = node.values[index - 1];

        //извлекаем значение для родителя (граничное) - последнее в левом ребенке (наибольшее) и помещаем в родителя
        //(вместе со значение ключа)
        node.keys[index - 1] = sibling.keys[sibling.cntKeys - 1];
        node.values[index - 1] = sibling.values[sibling.cntKeys - 1];

        //так как мы помещаем последний ключ из предыдущего ребенка достаточно просто уменьшить количество ключей в
        //предыущем и увеличить количество ключей в текущем (куда нам необходимо спуститься)
        child.cntKeys += 1;
        sibling.cntKeys -= 1;
        //синхронизируем изменения
        child.write();
        sibling.write();
        node.write();
    }

    //осуществление переноса одного элемента из правого ребенка в левый (из следующего в данный)
    //данный ребенок <- разделитель родителя <- первый элемент в следующем ребенке
    void getFromNext(int index, Node &node) {
        //локальные переменные для работы с текущим и следующим ребенком
        Node child = Node();
        Node sibling = Node();
        //считываем ребенка, в которого нам необходимо спуститься и следующего по счету
        child.read(node.children[index]);
        sibling.read(node.children[index + 1]);
        //перемещаем разделитель из родителя в последний элемент требуемого ребенка
        child.keys[child.cntKeys] = node.keys[index];
        child.values[child.cntKeys] = node.values[index];
        //если текущий ребенок не является листом - сдвигаем всех детей и вставляем на последнее место ветку наследований
        //(первую в следующем ребенке)
        if (!child.isLeaf)
            child.children[child.cntKeys + 1] = sibling.children[0];
        //перемещаем непосредственно первого ребенка на место разделителя, который на данный момент уже перемещен в
        //требуемого ребенка
        node.keys[index] = sibling.keys[0];
        node.values[index] = sibling.values[0];
        //сдвигаем ключи на один влево (так как мы извлекли один ключ из начала)
        for (int i = 1; i < sibling.cntKeys; ++i) {
            sibling.keys[i - 1] = sibling.keys[i];
            sibling.values[i - 1] = sibling.values[i];
        }
        //если следующий ребенок не является листом - сдвигаем всех его детей также как и ключи на одну позицию влево
        //аналогично ключам
        if (!sibling.isLeaf) {
            for (int i = 1; i <= sibling.cntKeys; ++i)
                sibling.children[i - 1] = sibling.children[i];
        }
        //увеличиваем количество ключей в требуемом ребенке и уменьшаем канутер ключей в следующем ребенке
        child.cntKeys += 1;
        sibling.cntKeys -= 1;
        //синхронизируем изменения
        child.write();
        sibling.write();
        node.write();

    }

    /*Описания обоих методов наиболее полно описаны в declaration в них
         * Основной смысл - заполение данного ребенка (в которого необходимо спуститься) элементом либо из правого либо из
         * леового соседа (смотрим по количеству ключей и индексу ребенка)
         * */
    void rebaseOrMerge(Node &node, int index) {
        auto child = Node();
        //отслеживаем чтобы дети, для которых необходимо вызвать заполнение из следующего или предыдущего ребенка имеют и правого и левого соседа
        //в данном случае индекс должен быть не нулевым, чтобы мы смогли обратиться к предыдущему ребенку
        if (index != 0 && child.read(node.children[index - 1]).cntKeys >= T)
            getFromPrevious(index, node);
            //индекс не должен быть равен количеству ключей, чтобы мы смогли обратиться к следующему ребенку
        else if (index != node.cntKeys && child.read(node.children[index + 1]).cntKeys >= T)
            getFromNext(index, node);
            // если ни там ни там мы не можем выполнить данную манипуляцию, то мы объединяем две ветки - merge
        else {
            //если невозможно добавить элемент из соседа - то мы восполняем недостаток ключей помещая в ребенка медиану
            //делаем действия обратные splitChild
            if (index != node.cntKeys)
                merge(index, node);
            else
                //-1 так как мы мерджим данного ребенка и следующего (соответсвтенно мы можем выйти за границу массива детей если этого не сделаем)
                merge(index - 1, node);
        }
    }

    //основной метод для удаления элемента из Б-дерева (ВАЖНО: Croot - копия корня для непосредлственно поиска
    // и манипуляций внутри рекурсии)
    bool deleteFromTree(int k, Node &Croot) {
        //вычисляем индекс элемента или (если на данном уровне элемента нет) вычисляем индекс ребенка в которого
        //необходимо спуститься для дальнейшего поиска и извлечения
        int index = findIndex(k, Croot);
        //если индекс находится на данном уровне (в рассматриваемом ноде) - удаляем его двумя способами описанными ниже
        if (index < Croot.cntKeys && Croot.keys[index] == k) {
            //если текущий нод является листом - удаляем элемент и сдвигаем массив (подробнее в описании метода)
            if (Croot.isLeaf) {
                leafRemove(Croot, index);
            } else {
                //если данный элемент не является листом:
                removeFromNode(Croot, index);
            }
        } else {
            //если мы попадаем в самого правого ребенка (индекс указывает на саомго правого ребенка)
            bool flag = index == Croot.cntKeys;
            //локальный нод для чтения ребенка и использования в рекурсии
            auto child = Node();
            //смотрим, есть ли необходимость восполнить ребенка (если в нем элементов t-1)
            if (child.read(Croot.children[index]).cntKeys < T) {
                //заполняем ребенка еще одним элементом (для того, чтобы иметь возомжность удалить
                //и не нарушить закон бдерева (каждый нод сожержит не меньше t-1 ключа))
                rebaseOrMerge(Croot, index);
            }
            //если мы забрали последний ключ как медиану при мердже (ключей стало меньше соотвтетсвенно на 1)
            if (flag && index > Croot.cntKeys) {
                return deleteFromTree(k, Croot.read(Croot.children[index - 1]));
            } else {
                //если ничего не произошло критичного - вызываем рекурсию еще раз (до тех пор пока не удалим нужный элемент)
                return deleteFromTree(k, Croot.read(Croot.children[index]));
            }
        }
        return true;
    }

public:
    //корень дерева
    Node root;

    BTree() {
        Node x = Node(nodeNumber++);
        x.isLeaf = true;
        x.write();
        root = x;
    }
    //поиск по дереву
    optional<pair<Node, int>> search(int k) {
        return node_search(root, k);
    }
    //вставка элемента в дерево
    bool treeInsert(size_t k, int value) {
        if (search(k).has_value())
            return false;
        //насколько успешно прошел insert
        bool m;
       // root.read(root.fileName);
        //текущий узел
        auto r = root;
        //если текущий узел полностью заполнен
        if (root.cntKeys == (2 * T - 1)) {
            //создаем нод
            //присваиваем текущий нод нашему узлу (растем вверх)
            root = Node(nodeNumber++);
            root.isLeaf = false;
            //записываем наш прошлый корень в ребенка новго корня (растем вверх)
            root.children[0] = r.fileName;
            //делим наш узел
            Node::treeSplitChild(root, 0, r);
            //вставляем наше значение в дерево
            m = treeInsertNonFull(root, k, value);
        } else {
            //если в узле есть место - вставляем в него значение
            m = treeInsertNonFull(r, k, value);
        }
        //считываем актуальный корень
        root.read(root.fileName);
        return m;
    }

    //удаление элемента из дерева
    bool beginToDelete(int k) {
        //копируем корень перед исполнением
        auto copy = root;
        //процесс удаления
        bool flag = deleteFromTree(k, copy);
        //обновляем корень
        root.read(root.fileName);
        //возвращаем результат
        return flag;
    }


};

int main(int argc, char *argv[]) {
    auto m = stoi(argv[1]);
    T = m;
    BTree tree;
    ifstream in{argv[3]};
    ofstream out{argv[4]};
    pathToFolder = argv[2];


    string command;
    int key, value;

    while (!in.eof()) {
        in >> command;
        //удаление
        if (command == "delete") {
            in >> key;
            //если значение в дереве действительно присутствует
            auto pair = tree.search(key);
            if (pair.has_value()) {
                //получение значения по данному ключу в конкретном ноде
                auto value1 = pair->first.values[pair->second];
                //удаление и вывод значения удаленного ключа
                if (tree.beginToDelete(key)) {
                    out << value1 << "\n";
                    }
                else
                {
                    cout << "null\n";
                }
            } else {
                //если удаление невозможно
                out << "null\n";
            }
            //вставка в дерево
        } else if (command == "insert") {
            //получение и вставка ключа и привязанного к нему значения
            in >> key >> value;
            out << (tree.treeInsert(key, value) ? "true\n" : "false\n");
            //поиск в дереве
        } else if (command == "find") {
            in >> key;
            //поиск по древу
            auto pair = tree.search(key);
            if (pair.has_value()) {
                //если элемент в дереве есть - выводим значение привязанное к ключу
                out << pair->first.values[pair->second] << "\n";
            } else {
                //если элемента в дереве нет - false
                out << "null\n";
            }
        }
    }
    return 0;
}


