# SmartCollider

Было необходимо реализовать зоны, осуществляющие фильтр всех входящих и выходящих объектов и в случае прохождения объектом фильтрации уведомляющие об этом систему. Спецификой задачи было, то, что у одного объекта могли находится дочерние объекты, которые так же имели свои Collider2D. В таком случае должно считаться, что в зону вошел только один родительский объект. Так же возникла проблема, с порядком вызова событий OnTriggerEnter/Exit: Unity не гарантирует, что эти события будут приходить в прямом порядке (событие выхода может произойти до события входа из-за специфики работы событийной модели Unity). Реализовал фильтрацию, а так же добавил дополнительную фильтрацию на проверку различного возможного состояния объекта при его внезапном исчезновении из зоны.

## Функционал 
* Фильтрация по необходимым классам
* Фильтрация по именам объектов
* Выбор событий на которые будет реагировать зона





Пример настройки зоны:  
![image](https://user-images.githubusercontent.com/51932532/137632468-eafd81b6-ab49-44c9-8cd0-42ae900dd1ee.png)

