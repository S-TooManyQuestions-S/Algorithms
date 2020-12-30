#include <iostream>
#include <fstream>
#include "ogrsf_frmts.h"
#include <gdal_priv.h>
#include "gdal.h"
#include "boost/geometry.hpp"

using namespace std;
//попытаемся избежать огромных названий при работе с библиотекой
using namespace boost::geometry;
//также заменим некотрые типы на более удобные и читабельные
using point = model::point<double, 2, cs::cartesian>;
using rectangle = model::box<point>;

//получение Minimal Border Rectangle
rectangle MBR(OGRPolygon* polygon){
    const shared_ptr<OGREnvelope> polygonEnvelope(new OGREnvelope);
    //используя методы из тестового репозитория
    polygon->getEnvelope(polygonEnvelope.get());
    return rectangle(point(polygonEnvelope->MinX, polygonEnvelope->MinY),
                     point(polygonEnvelope->MaxX,polygonEnvelope->MaxY));
}

//метод для обработки входных данных и построения прямоугольника
rectangle getByCoordinates(ifstream& in){
    double minX, minY, maxX, maxY;
    in >> minX >> minY >> maxX >> maxY;
    return rectangle (point(minX, minY),
                      point(maxX, maxY));
}
//компоратор для сортировки
bool CompareTo(pair<rectangle, int> a, pair<rectangle, int> b){
    return a.second < b.second;
}

int main(int argc, char *argv[]) {

    GDALAllRegister();
    //получаем входные данные из консоли
    string pathToTests = argv[2];
    string pathToAnswers = argv[3];

    auto* dataset = static_cast<GDALDataset*>(GDALOpenEx(
            argv[1],
            GDAL_OF_VECTOR,
            nullptr, nullptr, nullptr));

    if (dataset == nullptr) {
        cout << "Can not reach the file!" << endl;
        return -1;
    }
    //открываем потоки на чтение тестов и запись ответом по указанным в командной строке аргументам
    ifstream in {pathToTests};
    ofstream out{pathToAnswers};

    //считываем координаты прямоугольника из теста
    auto inputRec = getByCoordinates(in);
    //производим инициализацию RTree
    auto rTree = index::rtree<pair<rectangle, int> , index::quadratic<8, 4>>();
    //проходимся по датасету и заполняем RTree
    for (auto&& layer : dataset->GetLayers()){
        for (auto&& feature : layer){
            //получаем геометрию конкретного объекта
            auto* featureGeometry = feature->GetGeometryRef();
            //получаем osm_id объекта
            auto osm_id = feature->GetFieldAsInteger(0);
            //добавляем в р дерево пару (MBR (Minimal border rectangle) объекта и его айди)
            rTree.insert(make_pair(MBR(featureGeometry->toPolygon()), osm_id));
        }
    }
    //вектор для получения ответов
    vector<pair<rectangle, int>> pairs;
    //используя метод из условия задачи получаем все объекты пересекающиеся с данным прямоугольником
    rTree.query(boost::geometry::index::intersects(inputRec), back_inserter(pairs));
    sort(pairs.begin(), pairs.end(),  CompareTo);
    //выводим на экран
    for(auto onePair : pairs){
        out << onePair.second << endl;
    }
    return 0;
}
