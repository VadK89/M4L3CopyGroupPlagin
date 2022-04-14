using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace M4L3CopyGroupPlagin
{
    [TransactionAttribute(TransactionMode.Manual)]//
    public class CopyGroup : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;//получение ссылки на Юай документ
                Document doc = uiDoc.Document;//получение ссылки на экземпляр класса документ, соссылкой на бд открытого документ


                //экземпляр фильтра ввода для вобра групп
                GroupPickFilter groupPickFilter = new GroupPickFilter();

                //Выбор группы для копирования

                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберете группу объектов");//получение ссылки на объект
                Element element = doc.GetElement(reference); //получение самого объекта
                Group group = element as Group; //преобразование элемента в группу

                //поиск центра группы на основе ограничивающей рамки
                XYZ groupCenter = GetElementCenter(group);

                //определение комнаты в которой находится выбранная группа объектов
                Room room = GetRoomByPoint(doc, groupCenter);

                //поиск центра комнаты
                XYZ roomcenter = GetElementCenter(room);

                //поиск смещения центра группы относительно центра комнаты
                XYZ offset = groupCenter - roomcenter;

                //выбор комнаты для вставки
                RoomPickFilter roomPickFilter = new RoomPickFilter();
                
                Reference roomP = uiDoc.Selection.PickObject(ObjectType.Element, roomPickFilter, "Выберете комнату для копирования");//получение списка комнат

                //Room roompaste = GetRoomByPoint(doc, groupCenter);

                //XYZ roompastecenter = GetElementCenter(roompaste);
                ////Выбор точек вставки
                //XYZ point = groupCenter - roompastecenter;
                XYZ point = uiDoc.Selection.PickPoint("Выберете точку");
                
                //вставка группы в точку

                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы");
                //doc.Create.PlaceGroup(point, group.GroupType);
                //doc.Create.PlaceGroup(point, group.GroupType);
                Paster(doc, roomP, group.GroupType, offset);
                transaction.Commit();
            }
            //Исключение отмены операции
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            //исключение ошибка
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public void Paster(Document doc, Reference room, GroupType groupType, XYZ deviation)
        { 
            Room roompaste = doc.GetElement(room) as Room;

            if (roompaste != null)
            {
                XYZ roompastecenter = GetElementCenter(roompaste);
                doc.Create.PlaceGroup(roompastecenter + deviation, groupType);
            }
        }
        //метод для поиска цетра элемента
        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }
        //определение комнаты по исходной точке
        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            //для поиска
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            //перебор элементов
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }
            return null;
        }

        
    }

    //класс для фильтра выбора групп
    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            //необходимо возвращать true или false в зависимости от типа элемента
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
            return true;
            else
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
    public class RoomPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            //необходимо возвращать true или false в зависимости от типа элемента
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Rooms)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
