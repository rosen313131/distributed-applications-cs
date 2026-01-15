# Курсова работа

# HotelManagement System
**Факултетен номер:** 2301261017 
**Студент:** Росен Георгиев  
**Проект:** Hotel Management System 

## Описание на проекта
HotelManagement е уеб приложение, което позволява управление на хотелски стаи, резервации и гости.  
Поддържа три роли: Admin, User и Guest.  
Всяка роля има различни права за достъп.

## Функционалности
- Регистрация и логин на потребители  
- Роли: Admin, User  
- Admin може да създава, редактира и изтрива стаи и резервации  
- User може да прави резервации  
- Автоматично създаване на Guest записи  
- Търсене, филтриране и сортиране  
- Страница Information, достъпна за всички  


## Инструкции за стартиране

1. Клониране на проекта (Pull / Clone)
Отвори Git Bash, CMD или Visual Studio → Team Explorer → Clone.

git clone https://github.com/rosen313131/distributed-applications-cs

След това отвори решението:
distributed-applications-cs/course-work/implementations/HotelManagement/HotelManagement.sln

2.Настройване на базата данни
Отвори файла:appsettings.json
И провери ConnectionString:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HotelDB;Trusted_Connection=True;"
}
При нужда го смени според собствената ти SQL конфигурация.

3.В Package Manager Console изпълни:

Add-Migration InitialCreate
Update-Database

4. Старт на приложението

Натисни F5 или бутона Run.
