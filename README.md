# Alapiskolai Beiratkozási Formanyomtatvány Generátor

Ez a C# WPF alkalmazás lehetővé teszi alapiskolai beiratkozási adatlapok egyszerű generálását PDF és Word (DOCX) formátumban. Az alkalmazás segítségével gyorsan és egyszerűen kitölthetők az iskolai beiratkozáshoz szükséges nyomtatványok.

## Funkciók

- Tanulói adatok felvétele űrlapon keresztül
- Szülői adatok rögzítése
- Osztálytípus, tantárgy választás és egyéb iskolai paraméterek beállítása
- PDF és Word (DOCX) formátumú dokumentumok generálása egyetlen kattintással
- Formanyomtatványok automatikus mentése a választott helyre
- Dokumentumok opcionális megnyitása generálás után

## Rendszerkövetelmények

- Windows operációs rendszer
- .NET 8.0 vagy újabb
- Visual Studio 2022 vagy újabb (a fejlesztéshez)

## Telepítés

### Fejlesztői telepítés

1. Klónozza a repót: `git clone https://github.com/huser123/Alapiskola-beiratkozas-WPF`
2. Nyissa meg a Visual Studio-ban
3. Telepítse a szükséges NuGet csomagokat:
   - DocumentFormat.OpenXml
   - PdfSharp-wpf
   - System.Drawing.Common
   - System.Text.Encoding.CodePages
4. Fordítsa le és futtassa az alkalmazást

### Felhasználói telepítés

1. Töltse le a legújabb kiadást a Releases oldalról
2. Csomagolja ki a fájlokat
3. Futtassa az `IskolaiBeiratkozasGenerator.exe` fájlt

## Használat

1. Töltse ki az adatlapot a tanuló és a szülők adataival
2. Jelölje be a megfelelő osztálytípust, tantárgyat és egyéb opciókat
3. Kattintson a "Dokumentumok generálása" gombra
4. Válassza ki a mentés helyét
5. A program elkészíti és elmenti a dokumentumokat PDF és DOCX formátumban

## Fejlesztési lehetőségek

- Több nyelv támogatása
- Adatok mentése és betöltése későbbi használatra
- Több dokumentumtípus támogatása
- Batch feldolgozás több tanuló adataival

## Licensz

GNU General Public License v3.0

## Támogatás

Ha hibát talál vagy fejlesztési javaslata van, nyisson egy új Issue-t a GitHub oldalon.
