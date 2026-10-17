using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

class Cloud { public float X, Y, S; public int A; }
class Part { public float X, Y, VX, VY, S; public int L, M, Sh; public Color C; }
class Floater { public float X, Y; public int L; public string T; public bool Coin; }
class Item { public string Name, Kind; public int Price, Slot, Lvl, Mode, Power, MaxDur, Bonus, Pat; public Color L, D, P; }
class ShopRow { public RectangleF R; public int Item; }

class EggForm : Form
{
    Random rnd = new Random();
    float ang = 0, level = 0;
    int shake = 0, dodges = 0, tickN = 0, popTimer = 0;
    float eggScale = 1f, coinPulse = 0f;
    float shX = 0, shY = 0;
    int coins = 0, pops = 0, savedFlash = 0, lvl = 0, rebirthArm = 0, tab = 0;
    int hover = -1, hoverTab = -1;
    float mx = -200, my = -200, swing = 0;
    int toolBonus = 0, lastHitSound = -100;
    bool curHidden = false, spaceHeld = false, askName = false;
    string playerName = "", nameInput = "", nameError = "";
    int[] dur;
    bool hoverRebirth = false;
    string msg = "Något luktar här...";
    string btnText = "Rengör ägget";
    RectangleF btn = new RectangleF(125, 600, 170, 44);

    string[] msgs = {
        "Det här ägget har sett bättre dagar.",
        "Flugor har upptäckt din dator.",
        "Det luktar svavel från högtalaren...",
        "Ägget ångrar sig.",
        "Öppna ett fönster. Nu.",
        "Din dator har ruttnat med 12%.",
        "Någon i rummet har märkt något...",
        "Ägget bubblar. Det är inte bra.",
        "Ägget är äldre än din mormor."
    };

    // ---------- butikens saker ----------
    // Slots: 0 färg, 1 huvud, 2 ögon, 3 mun, 4 extra
    string[] tabNames = { "FÄRG", "HUVUD", "ÖGON", "MUN", "EXTRA", "VERKTYG", "KROPP", "HUSDJUR", "BAKGRUND", "POP", "TITEL" };
    List<Item> items = new List<Item>();
    bool[] owned;
    int[] equipped = { 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
    int cap = 1;
    Dictionary<int, SolidBrush> brushCache = new Dictionary<int, SolidBrush>();

    static Color RGB(int r, int g, int b) { return Color.FromArgb(r, g, b); }
    static Color Lerp(Color a, Color b, float t)
    {
        if (t < 0) t = 0; if (t > 1) t = 1;
        return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }
    SolidBrush SB(Color c)
    {
        SolidBrush b;
        int k = c.ToArgb();
        if (!brushCache.TryGetValue(k, out b)) { b = new SolidBrush(c); brushCache[k] = b; }
        return b;
    }

    void AddC(string n, int price, int lv, int mode, Color l, Color d)
    {
        Item it = new Item();
        it.Name = n; it.Price = price; it.Slot = 0; it.Lvl = lv; it.Mode = mode; it.L = l; it.D = d;
        items.Add(it);
    }
    void AddI(string n, int price, int slot, int lv)
    {
        Item it = new Item();
        it.Name = n; it.Price = price; it.Slot = slot; it.Lvl = lv;
        items.Add(it);
    }

    void AddT(string n, string kind, int price, int power, int maxDur, int bonus, int lv, Color head, Color handle)
    {
        Item it = new Item();
        it.Name = n; it.Kind = kind; it.Price = price; it.Slot = 5; it.Lvl = lv;
        it.Power = power; it.MaxDur = maxDur; it.Bonus = bonus; it.L = head; it.D = handle;
        items.Add(it);
    }

    void AddP(string n, int price, int lv, int pat, Color l, Color d, Color pc)
    {
        Item it = new Item();
        it.Name = n; it.Price = price; it.Slot = 0; it.Lvl = lv; it.Mode = 0; it.Pat = pat; it.L = l; it.D = d; it.P = pc;
        items.Add(it);
    }
    void AddS(string n, int price, int slot, int lv, int mode, Color l, Color d)
    {
        Item it = new Item();
        it.Name = n; it.Price = price; it.Slot = slot; it.Lvl = lv; it.Mode = mode; it.L = l; it.D = d;
        items.Add(it);
    }

    void InitItems()
    {
        // OBS: de första 11 ligger kvar i samma ordning som förut (för gamla sparfiler)
        AddC("Ruttet grön", 0, 0, 0, RGB(190, 200, 90), RGB(90, 110, 20));
        AddC("Guld", 50, 0, 0, RGB(255, 235, 130), RGB(190, 140, 10));
        AddC("Rosa", 40, 0, 0, RGB(255, 205, 225), RGB(220, 100, 150));
        AddC("Havsblå", 40, 0, 0, RGB(175, 220, 255), RGB(40, 100, 190));
        AddC("Lila", 60, 0, 0, RGB(220, 185, 255), RGB(110, 50, 170));
        AddC("Regnbåge", 150, 0, 1, Color.White, Color.Gray);
        AddI("Partyhatt", 30, 1, 0);
        AddI("Hög hatt", 70, 1, 0);
        AddI("Krona", 120, 1, 0);
        AddI("Solglasögon", 80, 2, 0);
        AddI("Mustasch", 50, 3, 0);
        // nya färger
        AddC("Mint", 80, 1, 0, RGB(200, 255, 225), RGB(40, 160, 110));
        AddC("Orange", 90, 1, 0, RGB(255, 210, 140), RGB(220, 100, 10));
        AddC("Svart", 150, 2, 0, RGB(110, 110, 120), RGB(15, 15, 20));
        AddC("Silver", 220, 3, 0, RGB(245, 245, 250), RGB(140, 150, 165));
        AddC("Neon", 300, 4, 2, Color.White, Color.Gray);
        AddC("Galax", 500, 6, 0, RGB(100, 70, 180), RGB(15, 10, 55));
        AddC("Diamant", 900, 8, 3, Color.White, Color.Gray);
        // nya huvudsaker
        AddI("Piratmössa", 100, 1, 1);
        AddI("Tomteluva", 130, 1, 2);
        AddI("Cowboyhatt", 160, 1, 2);
        AddI("Kockhatt", 200, 1, 3);
        AddI("Ängelring", 260, 1, 4);
        AddI("Djävulshorn", 260, 1, 4);
        AddI("Trollkarlshatt", 380, 1, 5);
        AddI("Propellerkeps", 450, 1, 6);
        AddI("Diamantkrona", 1200, 1, 8);
        // ögon
        AddI("Googly-ögon", 60, 2, 1);
        AddI("Runda glasögon", 90, 2, 1);
        AddI("Piratlapp", 140, 2, 3);
        AddI("Monokel", 170, 2, 3);
        AddI("Hjärtögon", 300, 2, 5);
        // mun
        AddI("Rosiga kinder", 80, 3, 2);
        AddI("Clownnäsa", 100, 3, 2);
        AddI("Läppar", 150, 3, 3);
        AddI("Vampyrtänder", 220, 3, 4);
        AddI("Skägg", 280, 3, 5);
        // extra
        AddI("Gnistor", 120, 4, 1);
        AddI("Stjärnor", 200, 4, 2);
        AddI("Hjärtan", 240, 4, 3);
        AddI("Snöfall", 300, 4, 4);
        AddI("Eld", 400, 4, 5);
        AddI("Åska", 500, 4, 6);
        AddI("Aura", 700, 4, 7);
        AddI("Regnbågsring", 1500, 4, 9);
        // ---- version 4 ----
        AddI("Kukirin G2", 500, 1, 2);
        AddI("Pizzaskiva", 150, 1, 2);
        AddI("Badanka", 160, 1, 2);
        AddI("Ufo", 500, 1, 6);
        AddI("Raket", 650, 1, 7);
        AddC("Choklad", 180, 3, 0, RGB(150, 95, 60), RGB(70, 35, 15));
        AddC("Is", 320, 4, 0, RGB(235, 252, 255), RGB(110, 180, 230));
        AddC("Lava", 450, 5, 4, Color.White, Color.Gray);
        AddC("Godis", 480, 6, 5, Color.White, Color.Gray);
        AddI("3D-glasögon", 110, 2, 2);
        AddI("Spiralögon", 130, 2, 3);
        AddI("Skidglasögon", 240, 2, 4);
        AddI("Laserögon", 350, 2, 5);
        AddI("Napp", 70, 3, 1);
        AddI("Tunga", 90, 3, 2);
        AddI("Tuggummibubbla", 150, 3, 3);
        AddI("Guldtand", 200, 3, 4);
        AddI("Stinkmoln", 100, 4, 1);
        AddI("Konfetti", 220, 4, 2);
        AddI("Musik", 260, 4, 3);
        AddI("Bubblor", 320, 4, 4);
        AddI("Disco", 900, 4, 8);
        // ---- version 5: verktyg (hugg) ----
        AddT("Bagett", "baguette", 25, 3, 25, 0, 3, RGB(225, 175, 95), RGB(150, 100, 40));
        AddT("Sked", "spoon", 40, 5, 40, 0, 3, RGB(200, 205, 215), RGB(170, 175, 185));
        AddT("Gaffel", "fork", 55, 7, 45, 0, 3, RGB(200, 205, 215), RGB(170, 175, 185));
        AddT("Träpickaxe", "pick", 90, 10, 50, 5, 3, RGB(190, 140, 70), RGB(130, 85, 40));
        AddT("Stekpanna", "pan", 120, 13, 60, 5, 3, RGB(70, 70, 78), RGB(50, 40, 35));
        AddT("Hammare", "hammer", 170, 17, 70, 10, 3, RGB(120, 125, 135), RGB(140, 95, 45));
        AddT("Stenyxa", "axe", 230, 22, 80, 10, 3, RGB(130, 130, 130), RGB(140, 95, 45));
        AddT("Järnpickaxe", "pick", 340, 30, 90, 15, 4, RGB(215, 215, 225), RGB(140, 95, 45));
        AddT("Järnyxa", "axe", 420, 38, 100, 20, 4, RGB(215, 215, 225), RGB(140, 95, 45));
        AddT("Baseballträ", "bat", 480, 44, 90, 20, 4, RGB(200, 150, 80), RGB(40, 40, 45));
        AddT("Stålsvärd", "sword", 700, 54, 110, 30, 5, RGB(225, 235, 245), RGB(90, 50, 25));
        AddT("Motorsåg", "chainsaw", 950, 64, 120, 40, 5, RGB(255, 130, 20), RGB(40, 40, 45));
        AddT("Guldpickaxe", "pick", 1300, 74, 130, 50, 6, RGB(255, 215, 60), RGB(140, 95, 45));
        AddT("Trollstav", "wand", 1700, 84, 140, 60, 6, RGB(255, 225, 80), RGB(110, 50, 170));
        AddT("Diamantpickaxe", "pick", 2300, 94, 160, 80, 7, RGB(90, 230, 240), RGB(140, 95, 45));
        AddT("Ljussabel", "saber", 3200, 110, 200, 100, 8, RGB(70, 255, 130), RGB(150, 155, 165));
        // ================= version 11 =================
        // färger
        AddC("Citron", 60, 1, 0, RGB(255, 250, 150), RGB(200, 190, 40));
        AddC("Persika", 70, 1, 0, RGB(255, 220, 190), RGB(240, 150, 110));
        AddC("Lime", 120, 2, 0, RGB(220, 255, 150), RGB(100, 190, 30));
        AddC("Korall", 130, 2, 0, RGB(255, 190, 180), RGB(230, 90, 90));
        AddC("Turkos", 200, 3, 0, RGB(190, 255, 245), RGB(30, 170, 160));
        AddC("Indigo", 220, 3, 0, RGB(190, 170, 255), RGB(50, 40, 150));
        AddC("Bordeaux", 300, 4, 0, RGB(200, 90, 110), RGB(80, 10, 35));
        AddC("Skogsgrön", 320, 4, 0, RGB(150, 200, 130), RGB(25, 80, 40));
        AddC("Rost", 420, 5, 0, RGB(230, 170, 120), RGB(120, 50, 20));
        AddC("Himmelsblå", 440, 5, 0, RGB(210, 240, 255), RGB(70, 150, 230));
        AddC("Roséguld", 600, 6, 0, RGB(255, 215, 200), RGB(200, 130, 110));
        AddC("Midnatt", 650, 6, 0, RGB(90, 90, 160), RGB(10, 10, 50));
        AddC("Giftgrön", 800, 7, 0, RGB(190, 255, 120), RGB(40, 160, 0));
        AddP("Zebra", 180, 2, 4, RGB(255, 255, 255), RGB(225, 225, 230), RGB(30, 30, 30));
        AddP("Dalmatiner", 260, 3, 2, RGB(255, 255, 255), RGB(225, 225, 230), RGB(30, 30, 30));
        AddP("Rutig", 280, 3, 3, RGB(245, 245, 245), RGB(205, 205, 210), RGB(200, 40, 40));
        AddP("Tiger", 380, 4, 4, RGB(255, 190, 90), RGB(230, 120, 20), RGB(30, 20, 10));
        AddP("Hjärtmönster", 500, 5, 6, RGB(255, 200, 220), RGB(240, 120, 160), RGB(255, 255, 255));
        AddP("Stjärnmönster", 520, 5, 7, RGB(60, 70, 160), RGB(20, 20, 80), RGB(255, 230, 100));
        AddP("Kamouflage", 700, 6, 8, RGB(150, 170, 100), RGB(90, 110, 60), RGB(55, 75, 35));
        AddP("Randigt godis", 900, 7, 9, RGB(255, 255, 255), RGB(240, 240, 245), RGB(230, 40, 60));
        AddP("Zick-zack", 1100, 8, 5, RGB(255, 230, 120), RGB(240, 180, 40), RGB(200, 30, 30));
        AddC("Aurora", 2500, 9, 6, Color.White, Color.Gray);
        AddC("Blixt", 3500, 10, 7, Color.White, Color.Gray);
        AddC("Hologram", 6000, 12, 8, Color.White, Color.Gray);
        AddC("Plasma", 9000, 14, 9, Color.White, Color.Gray);
        AddC("Svart hål", 14000, 16, 10, Color.White, Color.Gray);
        // huvud
        AddI("Basker", 140, 1, 1);
        AddI("Keps", 120, 1, 1);
        AddI("Viking", 180, 1, 2);
        AddI("Sombrero", 200, 1, 2);
        AddI("Glass", 260, 1, 3);
        AddI("Svamp", 300, 1, 3);
        AddI("Kattöron", 300, 1, 3);
        AddI("Ballong", 280, 1, 3);
        AddI("Cupcake", 320, 1, 4);
        AddI("Kaninöron", 350, 1, 4);
        AddI("Lampa", 450, 1, 5);
        AddI("Popcorn", 500, 1, 5);
        AddI("Pannkakor", 420, 1, 5);
        AddI("Fjäderkrona", 600, 1, 6);
        AddI("Astronauthjälm", 900, 1, 8);
        AddI("Eldkrona", 1800, 1, 10);
        AddI("Saturnus", 2500, 1, 12);
        // ögon
        AddI("Kryssögon", 50, 2, 1);
        AddI("Argsint", 70, 2, 1);
        AddI("Sömnögon", 90, 2, 2);
        AddI("Katögon", 150, 2, 2);
        AddI("Stjärnögon", 200, 2, 3);
        AddI("Tårar", 220, 2, 3);
        AddI("Dollarögon", 300, 2, 4);
        AddI("Cyklop", 350, 2, 4);
        AddI("Tredje ögat", 600, 2, 6);
        AddI("Robotögon", 900, 2, 8);
        // mun
        AddI("Leende", 40, 3, 1);
        AddI("Överraskad", 60, 3, 1);
        AddI("Grisnos", 100, 3, 2);
        AddI("Näbb", 120, 3, 2);
        AddI("Tandgrill", 200, 3, 3);
        AddI("Pipa", 220, 3, 3);
        AddI("Munskydd", 150, 3, 4);
        AddI("Hajtänder", 300, 3, 4);
        AddI("Snabel", 400, 3, 5);
        AddI("Långt skägg", 500, 3, 6);
        // extra
        AddI("Fjärilar", 150, 4, 1);
        AddI("Regnmoln", 200, 4, 2);
        AddI("Spöken", 300, 4, 3);
        AddI("Planeter", 400, 4, 4);
        AddI("Eldklot", 500, 4, 5);
        AddI("Månljus", 600, 4, 6);
        AddI("Tornado", 1000, 4, 8);
        AddI("Laserstrålar", 1500, 4, 9);
        // verktyg
        AddT("Obsidianpickaxe", "pick", 4800, 130, 180, 120, 9, RGB(80, 40, 120), RGB(50, 35, 25));
        AddT("Rubinpickaxe", "pick", 6500, 160, 190, 150, 10, RGB(230, 40, 70), RGB(50, 35, 25));
        AddT("Eldyxa", "axe", 8500, 190, 200, 180, 11, RGB(255, 120, 20), RGB(60, 40, 25));
        AddT("Isyxa", "axe", 8500, 190, 200, 180, 11, RGB(170, 235, 255), RGB(60, 40, 25));
        AddT("Kristallsvärd", "sword", 12000, 220, 220, 220, 12, RGB(200, 140, 255), RGB(90, 50, 130));
        AddT("Skuggsvärd", "sword", 15000, 250, 240, 250, 13, RGB(70, 40, 90), RGB(30, 20, 40));
        AddT("Skördare", "scythe", 19000, 290, 250, 280, 14, RGB(200, 210, 220), RGB(100, 70, 40));
        AddT("Treudd", "trident", 24000, 330, 260, 320, 15, RGB(80, 220, 230), RGB(170, 150, 70));
        AddT("Elgitarr", "guitar", 30000, 380, 280, 360, 16, RGB(230, 40, 50), RGB(60, 40, 30));
        AddT("Plasmasabel", "saber", 38000, 430, 300, 400, 18, RGB(255, 80, 230), RGB(150, 155, 165));
        AddT("Smaragdsabel", "saber", 50000, 490, 330, 450, 20, RGB(30, 255, 170), RGB(150, 155, 165));
        // kropp
        AddI("Hängslen", 90, 6, 1);
        AddI("Fluga", 120, 6, 1);
        AddI("Tröja", 160, 6, 2);
        AddI("Halsduk", 200, 6, 2);
        AddI("Cape", 350, 6, 3);
        AddI("Tutu", 300, 6, 3);
        AddI("Smoking", 500, 6, 4);
        AddI("Armar", 600, 6, 5);
        AddI("Rustning", 900, 6, 6);
        AddI("Fjärilsvingar", 1500, 6, 7);
        AddI("Gångben", 1200, 6, 7);
        AddI("Änglavingar", 2500, 6, 9);
        AddI("Jetpack", 4000, 6, 11);
        AddI("Drakvingar", 7000, 6, 13);
        // husdjur
        AddI("Kyckling", 150, 7, 1);
        AddI("Bi", 200, 7, 2);
        AddI("Katt", 300, 7, 3);
        AddI("Pingvin", 400, 7, 4);
        AddI("Spöke", 550, 7, 5);
        AddI("Uggla", 700, 7, 6);
        AddI("Fisk", 900, 7, 7);
        AddI("Drönare", 1200, 7, 8);
        AddI("Rymdvarelse", 2000, 7, 10);
        AddI("Drake", 3500, 7, 12);
        // bakgrunder
        AddS("Natt", 300, 8, 1, 1, RGB(10, 15, 50), RGB(40, 30, 90));
        AddS("Solnedgång", 400, 8, 2, 0, RGB(255, 150, 60), RGB(120, 40, 120));
        AddS("Äng", 500, 8, 3, 2, RGB(140, 200, 255), RGB(190, 235, 150));
        AddS("Molnhimmel", 500, 8, 3, 8, RGB(70, 150, 240), RGB(190, 225, 255));
        AddS("Hav", 700, 8, 4, 3, RGB(120, 200, 255), RGB(10, 60, 140));
        AddS("Snöland", 900, 8, 5, 4, RGB(150, 180, 220), RGB(225, 235, 250));
        AddS("Rymd", 1400, 8, 6, 1, RGB(0, 0, 10), RGB(30, 10, 60));
        AddS("Stad", 2000, 8, 7, 6, RGB(30, 20, 70), RGB(220, 110, 100));
        AddS("Retrogrid", 3000, 8, 8, 7, RGB(20, 0, 60), RGB(120, 20, 120));
        AddS("Undervatten", 4500, 8, 9, 9, RGB(30, 150, 190), RGB(5, 30, 90));
        AddS("Vulkan", 6000, 8, 10, 5, RGB(40, 0, 0), RGB(160, 30, 0));
        AddS("Öken", 8000, 8, 11, 10, RGB(255, 200, 120), RGB(255, 235, 170));
        // pop-effekter
        AddS("Konfetti-pop", 400, 9, 2, 1, RGB(255, 80, 150), RGB(80, 200, 255));
        AddS("Guldregn", 700, 9, 3, 2, RGB(255, 215, 60), RGB(255, 170, 20));
        AddS("Hjärtexplosion", 900, 9, 4, 3, RGB(255, 70, 120), RGB(255, 150, 190));
        AddS("Snöstorm", 1300, 9, 5, 5, RGB(240, 250, 255), RGB(150, 200, 255));
        AddS("Bubbelpop", 1300, 9, 5, 9, RGB(180, 230, 255), RGB(90, 170, 255));
        AddS("Stjärnfall", 2000, 9, 6, 8, RGB(255, 235, 100), RGB(255, 255, 255));
        AddS("Eldboll", 3000, 9, 7, 6, RGB(255, 160, 30), RGB(220, 40, 0));
        AddS("Fyrverkeri", 5000, 9, 9, 4, RGB(255, 60, 60), RGB(60, 200, 255));
        AddS("Regnbågsregn", 9000, 9, 11, 7, RGB(255, 80, 80), RGB(80, 80, 255));
        // titlar
        AddS("Ägg-nybörjare", 20, 10, 0, 0, RGB(210, 210, 210), Color.Gray);
        AddS("Gulan", 100, 10, 1, 0, RGB(255, 225, 80), Color.Gray);
        AddS("Stinkare", 250, 10, 2, 0, RGB(160, 220, 70), Color.Gray);
        AddS("Hugg-lärling", 500, 10, 3, 0, RGB(230, 170, 90), Color.Gray);
        AddS("Ägg-krossaren", 900, 10, 4, 0, RGB(255, 130, 70), Color.Gray);
        AddS("Äggmästare", 1500, 10, 5, 0, RGB(110, 200, 255), Color.Gray);
        AddS("Stinkdrottning", 2500, 10, 6, 0, RGB(255, 120, 200), Color.Gray);
        AddS("Omelett-kungen", 4000, 10, 7, 0, RGB(255, 200, 60), Color.Gray);
        AddS("Skolans skräck", 6000, 10, 8, 0, RGB(255, 80, 80), Color.Gray);
        AddS("Rebirth-riddare", 9000, 10, 9, 0, RGB(170, 150, 255), Color.Gray);
        AddS("Guldägget", 14000, 10, 10, 0, RGB(255, 215, 0), Color.Gray);
        AddS("Ruttet till kärnan", 20000, 10, 11, 0, RGB(120, 255, 120), Color.Gray);
        AddS("Ägg-legend", 40000, 10, 13, 0, RGB(255, 180, 255), Color.Gray);
        AddS("Odödlig", 80000, 10, 15, 0, RGB(150, 255, 255), Color.Gray);
        AddS("Äggguden", 200000, 10, 18, 0, RGB(255, 240, 160), Color.Gray);
        AddS("Världens bästa", 500000, 10, 20, 0, RGB(255, 255, 255), Color.Gray);

        int[] cnt = new int[11];
        foreach (Item it in items) cnt[it.Slot]++;
        foreach (int c in cnt) if (c > cap) cap = c;
        owned = new bool[items.Count];
        dur = new int[items.Count];
    }

    void ItemColors(Item it, out Color l, out Color d)
    {
        float tt = (float)((Math.Sin(ang * 2) + 1) / 2);
        if (it.Mode == 1) { l = Hsv(ang * 40, 0.4, 1.0); d = Hsv(ang * 40 + 40, 0.9, 0.75); }
        else if (it.Mode == 2) { l = Lerp(RGB(150, 255, 80), RGB(255, 100, 220), tt); d = Lerp(RGB(0, 160, 60), RGB(150, 0, 180), tt); }
        else if (it.Mode == 3) { l = Lerp(RGB(230, 250, 255), RGB(160, 230, 255), tt); d = Lerp(RGB(90, 190, 230), RGB(200, 240, 255), tt); }
        else if (it.Mode == 4) { l = Lerp(RGB(255, 225, 70), RGB(255, 140, 0), tt); d = Lerp(RGB(210, 40, 0), RGB(120, 0, 0), tt); }
        else if (it.Mode == 5) { l = Hsv(ang * 20 + 300, 0.25, 1.0); d = Hsv(ang * 20 + 300, 0.6, 0.95); }
        else if (it.Mode == 6)
        {
            float ta = (float)((Math.Sin(ang) + 1) / 2);
            l = Lerp(RGB(120, 255, 200), RGB(200, 120, 255), ta);
            d = Lerp(RGB(10, 100, 90), RGB(60, 20, 120), ta);
        }
        else if (it.Mode == 7)
        {
            float fk = (Math.Sin(ang * 25) > 0.7) ? 1f : 0f;
            l = Lerp(RGB(60, 60, 90), RGB(255, 255, 200), fk);
            d = Lerp(RGB(20, 20, 40), RGB(255, 230, 60), fk);
        }
        else if (it.Mode == 8) { l = Hsv(ang * 70 + 180, 0.35, 1.0); d = Hsv(ang * 70 + 240, 0.7, 0.9); }
        else if (it.Mode == 9)
        {
            float t4 = (float)((Math.Sin(ang * 4) + 1) / 2);
            l = Lerp(RGB(255, 120, 255), RGB(120, 255, 255), t4);
            d = Lerp(RGB(120, 0, 160), RGB(0, 120, 180), t4);
        }
        else if (it.Mode == 10)
        {
            float t5 = (float)((Math.Sin(ang * 3) + 1) / 2);
            l = Lerp(RGB(30, 0, 50), RGB(130, 40, 200), t5);
            d = RGB(0, 0, 0);
        }
        else { l = it.L; d = it.D; }
    }

    void EggColors(out Color lc, out Color dc)
    {
        ItemColors(items[equipped[0]], out lc, out dc);
    }

    // ---------- rebirth ----------
    float Mult() { return 1f + 0.1f * lvl; }
    int RebirthCost() { return (int)(Math.Round(1000.0 * Math.Pow(1.4, lvl) / 50.0) * 50.0); }

    void DoRebirth()
    {
        int cost = RebirthCost();
        if (coins < cost)
        {
            msg = "Rebirth kostar " + cost + " coins. Du har " + coins + ".";
            rebirthArm = 0;
            return;
        }
        if (rebirthArm <= 0)
        {
            rebirthArm = 100;
            msg = "Klicka på REBIRTH igen för att bekräfta!";
            return;
        }
        coins -= cost;
        lvl++;
        level = 0;
        rebirthArm = 0;
        int fresh = 0;
        foreach (Item it in items) if (it.Lvl == lvl) fresh++;
        msg = "REBIRTH! Nivå " + lvl + " - " + fresh + " nya saker i butiken!";
        coinPulse = 1f;
        shake = 14;
        for (int i = 0; i < 80; i++)
        {
            Part p = new Part();
            double a = rnd.NextDouble() * Math.PI * 2;
            float sp = 3 + (float)rnd.NextDouble() * 10;
            p.X = 210; p.Y = 330;
            p.VX = (float)Math.Cos(a) * sp;
            p.VY = (float)Math.Sin(a) * sp - 4;
            p.S = 6 + (float)rnd.NextDouble() * 10;
            p.L = p.M = 35 + rnd.Next(30);
            p.C = (rnd.Next(2) == 0) ? Color.FromArgb(255, 214, 64) : Color.FromArgb(255, 245, 160);
            parts.Add(p);
        }
        Floater f = new Floater();
        f.X = 210; f.Y = 230; f.L = 90; f.T = "NIVÅ " + lvl; f.Coin = false;
        floats.Add(f);
        Sound(3);
        Save(true);
    }

    List<Cloud> clouds = new List<Cloud>();
    List<Part> parts = new List<Part>();
    List<Floater> floats = new List<Floater>();
    System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();

    string[] SavePaths()
    {
        return new string[] {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RuttetAgg.txt"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RuttetAgg-sparfil.txt") };
    }

    // allt i spelscenen ritas i en virtuell yta på 420x700 som skalas till skärmen
    float sc = 1, ox = 0, oy = 0;
    void DoLayout()
    {
        sc = Math.Min(ClientSize.Width / 420f, ClientSize.Height / 700f);
        ox = (ClientSize.Width - 420f * sc) / 2f;
        oy = (ClientSize.Height - 700f * sc) / 2f;
    }
    float VX(float x) { return (x - ox) / sc; }
    float VY(float y) { return (y - oy) / sc; }
    float UI()
    {
        float u = ClientSize.Height / 1080f;
        if (u < 0.6f) u = 0.6f;
        if (u > 1.3f) u = 1.3f;
        return u;
    }

    public EggForm()
    {
        Text = "Ruttet ägg";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        TopMost = true;
        BackColor = Color.FromArgb(50, 60, 16);
        DoubleBuffered = true;
        InitItems();
        owned[0] = true;
        LoadSave();
        askName = (playerName.Length < 2);
        DoLayout();

        MouseDown += OnDown;
        MouseMove += OnMove;
        t.Interval = 30;
        t.Tick += Tick;
        t.Start();
        StartUpdateCheck();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        DoLayout();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (askName) { if (e.KeyCode == Keys.Escape) Close(); return; }
        if (e.KeyCode == Keys.Escape) Close();
        else if (e.KeyCode == Keys.Space)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            bool first = !spaceHeld;
            spaceHeld = true;
            if (first && popTimer == 0 && eggScale > 0.5f)
                HitEgg(210f + (float)(rnd.NextDouble() * 50 - 25), 330f + (float)(rnd.NextDouble() * 70 - 35));
        }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (!askName) return;
        char ch = e.KeyChar;
        if (ch == (char)8)
        {
            if (nameInput.Length > 0) nameInput = nameInput.Substring(0, nameInput.Length - 1);
            nameError = "";
        }
        else if (ch == '\r' || ch == '\n') SubmitName();
        else if (nameInput.Length < 16 && (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-' || ch == '_' || ch == '.'))
        {
            nameInput += ch;
            nameError = "";
        }
        e.Handled = true;
    }

    void SubmitName()
    {
        string n = nameInput.Trim();
        while (n.Contains("  ")) n = n.Replace("  ", " ");
        if (n.Length < 2) { nameError = "Namnet måste vara minst 2 tecken."; return; }
        playerName = n;
        askName = false;
        nameError = "";
        Save(true);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (askName) return;
        BuildShop();
        if (shopPanel.Contains(e.X, e.Y))
        {
            scroll[tab] -= (e.Delta > 0 ? 2 : -2);
            if (scroll[tab] < 0) scroll[tab] = 0;
            if (scroll[tab] > shopMax) scroll[tab] = shopMax;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode == Keys.Space) spaceHeld = false;
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        spaceHeld = false;
    }

    // ---------- spara / ladda ----------
    void Save(bool flash)
    {
        try
        {
            string o = "";
            for (int i = 0; i < owned.Length; i++) o += owned[i] ? "1" : "0";
            string eq = "";
            for (int i = 0; i < equipped.Length; i++) eq += (i > 0 ? "," : "") + (equipped[i] + 1);
            string du = "";
            for (int i = 0; i < dur.Length; i++) du += (i > 0 ? "," : "") + dur[i];
            string txt = "coins=" + coins + "\r\nowned=" + o + "\r\neq=" + eq + "\r\ndur=" + du + "\r\nname=" + playerName +
                "\r\nlvl=" + lvl + "\r\nlevel=" + (int)Math.Min(level, 99f) +
                "\r\npops=" + pops + "\r\ndodges=" + dodges + "\r\n";
            foreach (string path in SavePaths())
            {
                try
                {
                    File.WriteAllText(path, txt);
                    if (flash) savedFlash = 60;
                    return;
                }
                catch { }
            }
        }
        catch { }
    }

    void LoadSave()
    {
        foreach (string path in SavePaths())
        {
            try
            {
                if (File.Exists(path) && ParseSave(File.ReadAllText(path))) return;
            }
            catch { }
        }
    }

    bool ParseSave(string text)
    {
        try
        {
            string o; int c;
            List<int> eq = new List<int>();
            int nLvl = 0, nPops = 0, nDodges = 0, nStink = 0;
            string nDur = null, nName = "";
            if (text.IndexOf('|') >= 0) // gammalt format
            {
                string[] p = text.Split('|');
                c = int.Parse(p[0].Trim());
                o = p[1].Trim();
                foreach (string s in p[2].Trim().Split(',')) eq.Add(int.Parse(s.Trim()) - 1);
            }
            else
            {
                Dictionary<string, string> d = new Dictionary<string, string>();
                foreach (string line in text.Split('\n'))
                {
                    int k = line.IndexOf('=');
                    if (k > 0) d[line.Substring(0, k).Trim()] = line.Substring(k + 1).Trim();
                }
                c = int.Parse(d["coins"]);
                o = d["owned"];
                foreach (string s in d["eq"].Split(',')) eq.Add(int.Parse(s.Trim()) - 1);
                if (d.ContainsKey("lvl")) nLvl = int.Parse(d["lvl"]);
                if (d.ContainsKey("level")) nStink = int.Parse(d["level"]);
                if (d.ContainsKey("pops")) nPops = int.Parse(d["pops"]);
                if (d.ContainsKey("dodges")) nDodges = int.Parse(d["dodges"]);
                if (d.ContainsKey("dur")) nDur = d["dur"];
                if (d.ContainsKey("name")) nName = d["name"];
            }
            for (int i = 0; i < owned.Length; i++) owned[i] = (i < o.Length && o[i] == '1');
            owned[0] = true;
            for (int i = 0; i < dur.Length; i++) dur[i] = 0;
            if (nDur != null)
            {
                string[] dp = nDur.Split(',');
                for (int i = 0; i < dp.Length && i < dur.Length; i++)
                {
                    int dv;
                    if (int.TryParse(dp[i], out dv)) dur[i] = dv;
                }
            }
            for (int i = 0; i < items.Count; i++)
                if (items[i].Slot == 5)
                {
                    if (!owned[i]) dur[i] = 0;
                    else if (dur[i] <= 0) dur[i] = items[i].MaxDur;
                }
            nName = nName.Trim();
            if (nName.Length > 16) nName = nName.Substring(0, 16);
            playerName = nName;
            coins = c; lvl = nLvl; pops = nPops; dodges = nDodges;
            level = Math.Min(99, nStink);
            if (dodges >= 12) btnText = "Går inte att rengöra";
            else if (dodges >= 8) btnText = "Ge upp?";
            else if (dodges >= 4) btnText = "Okej, okej...";
            for (int k = 0; k < equipped.Length; k++) equipped[k] = (k == 0) ? 0 : -1;
            foreach (int idx in eq)
                if (idx >= 0 && idx < owned.Length && owned[idx]) equipped[items[idx].Slot] = idx;
            return true;
        }
        catch { return false; }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (curHidden) { Cursor.Show(); curHidden = false; }
        Save(false);
        base.OnFormClosing(e);
    }

    static void LogError(Exception ex)
    {
        try
        {
            File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RuttetAgg-fel.txt"),
                DateTime.Now.ToString() + "  " + ex.ToString() + "\r\n");
        }
        catch { }
    }

    // ---------- ljud ----------
    void Sound(int kind)
    {
        Thread th = new Thread(delegate () {
            try
            {
                if (kind == 0) { for (int f = 180; f > 45; f -= 12) Console.Beep(f, 35); }
                else if (kind == 1) { for (int f = 220; f > 45; f -= 10) Console.Beep(f, 30); Console.Beep(880, 70); Console.Beep(1320, 120); }
                else if (kind == 2) { Console.Beep(700, 60); Console.Beep(1000, 60); Console.Beep(1400, 100); }
                else if (kind == 4) { Console.Beep(300, 60); Console.Beep(180, 90); Console.Beep(90, 160); }
                else { Console.Beep(600, 90); Console.Beep(800, 90); Console.Beep(1000, 90); Console.Beep(1300, 90); Console.Beep(1700, 220); }
            }
            catch { }
        });
        th.IsBackground = true;
        th.Start();
    }

    // ---------- knapp som flyr ----------
    void Dodge()
    {
        dodges++;
        float x0 = VX(0) + 10, x1 = VX(ClientSize.Width * 0.7f) - btn.Width - 10;
        if (x1 < x0) { x0 = 10; x1 = 240; }
        btn.X = x0 + (float)rnd.NextDouble() * (x1 - x0);
        btn.Y = 520 + (float)rnd.NextDouble() * 120;
        if (dodges == 4) btnText = "Okej, okej...";
        if (dodges == 8) btnText = "Ge upp?";
        if (dodges == 12) btnText = "Går inte att rengöra";
    }

    // ---------- butik ----------
    RectangleF shopPanel, shopRebirth;
    RectangleF[] shopTabs = new RectangleF[11];
    int shopMax = 0, shopCount = 0;
    int[] scroll = new int[11];
    const int VISROWS = 13;
    RectangleF shopList;
    List<ShopRow> shopRows = new List<ShopRow>();

    void BuildShop()
    {
        float u = UI();
        float pw = 380 * u, px = ClientSize.Width - pw - 20 * u, py = 80 * u;
        shopRows.Clear();
        shopRebirth = new RectangleF(px + 10 * u, py + 44 * u, pw - 20 * u, 48 * u);
        int nt = tabNames.Length;
        for (int i = 0; i < nt; i++)
        {
            int cols = (i < 6) ? 6 : (nt - 6);
            int ci = (i < 6) ? i : (i - 6);
            float tw = (pw - 20 * u) / cols;
            shopTabs[i] = new RectangleF(px + 10 * u + ci * tw, py + ((i < 6) ? 98 : 130) * u, tw - 2 * u, 28 * u);
        }
        List<int> ids = new List<int>();
        for (int i = 0; i < items.Count; i++) if (items[i].Slot == tab) ids.Add(i);
        ids.Sort(delegate (int a, int b)
        {
            int c = items[a].Lvl.CompareTo(items[b].Lvl);
            if (c != 0) return c;
            return items[a].Price.CompareTo(items[b].Price);
        });
        shopCount = ids.Count;
        shopMax = Math.Max(0, ids.Count - VISROWS);
        if (scroll[tab] > shopMax) scroll[tab] = shopMax;
        if (scroll[tab] < 0) scroll[tab] = 0;
        float listTop = py + 166 * u;
        shopList = new RectangleF(px + 10 * u, listTop, pw - 20 * u, VISROWS * 42 * u);
        for (int idx = scroll[tab]; idx < ids.Count && idx < scroll[tab] + VISROWS; idx++)
        {
            ShopRow r = new ShopRow();
            r.Item = ids[idx];
            r.R = new RectangleF(px + 10 * u, listTop + (idx - scroll[tab]) * 42 * u, pw - 20 * u - 12 * u, 38 * u);
            shopRows.Add(r);
        }
        shopPanel = new RectangleF(px, py, pw, (166 + VISROWS * 42 + 8) * u);
    }

    bool TabHasNew(int s)
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i].Slot == s && items[i].Lvl > 0 && items[i].Lvl == lvl && !owned[i]) return true;
        return false;
    }

    void ClickItem(int i)
    {
        Item it = items[i];
        int slot = it.Slot;
        if (it.Lvl > lvl)
        {
            msg = it.Name + " låses upp på nivå " + it.Lvl + " - gör en rebirth!";
            return;
        }
        if (owned[i])
        {
            if (slot == 0) equipped[0] = i;
            else equipped[slot] = (equipped[slot] == i) ? -1 : i;
        }
        else if (coins >= it.Price)
        {
            coins -= it.Price;
            owned[i] = true;
            if (it.Slot == 5) dur[i] = it.MaxDur;
            equipped[slot] = i;
            msg = "Köpt: " + it.Name + "!";
            coinPulse = 1f;
            Sound(2);
        }
        else
        {
            msg = "Du har inte råd med " + it.Name + "... Poppa fler ägg!";
        }
        Save(true);
    }

    // ---------- mus ----------
    bool OverEgg(float vx, float vy)
    {
        float dx = (vx - 210) / 80f, dy = (vy - 330) / 105f;
        return dx * dx + dy * dy <= 1 && popTimer == 0 && eggScale > 0.5f;
    }

    void OnMove(object s, MouseEventArgs e)
    {
        BuildShop();
        hover = -1; hoverTab = -1;
        foreach (ShopRow r in shopRows)
            if (r.R.Contains(e.X, e.Y)) hover = r.Item;
        for (int i = 0; i < tabNames.Length; i++)
            if (shopTabs[i].Contains(e.X, e.Y)) hoverTab = i;
        hoverRebirth = shopRebirth.Contains(e.X, e.Y);
        float vx = VX(e.X), vy = VY(e.Y);
        mx = e.X; my = e.Y;
        bool hand = (hover >= 0 || hoverTab >= 0 || hoverRebirth || btn.Contains(vx, vy) || OverEgg(vx, vy));
        bool wantHide = (equipped[5] >= 0 && !shopPanel.Contains(e.X, e.Y));
        if (wantHide)
        {
            Cursor = Cursors.Default;
            if (!curHidden) { Cursor.Hide(); curHidden = true; }
        }
        else
        {
            if (curHidden) { Cursor.Show(); curHidden = false; }
            Cursor = hand ? Cursors.Hand : Cursors.Default;
        }
        if (btn.Contains(vx, vy)) Dodge();
    }

    void OnDown(object s, MouseEventArgs e)
    {
        if (askName) return;
        BuildShop();
        for (int i = 0; i < tabNames.Length; i++)
            if (shopTabs[i].Contains(e.X, e.Y)) { tab = i; return; }
        if (shopRebirth.Contains(e.X, e.Y)) { DoRebirth(); return; }
        foreach (ShopRow r in shopRows)
            if (r.R.Contains(e.X, e.Y)) { ClickItem(r.Item); return; }

        float vx = VX(e.X), vy = VY(e.Y);
        if (btn.Contains(vx, vy))
        {
            msg = "Det fastnade. Ägget är nu en del av datorn.";
            level += 10;
            return;
        }
        if (OverEgg(vx, vy)) HitEgg(vx, vy);
    }

    // ---------- hugg med verktyg ----------
    void HitEgg(float vx, float vy)
    {
        int gain = 12;
        Color lc, dc;
        EggColors(out lc, out dc);
        for (int k = 0; k < 5; k++)
        {
            Part sh = new Part();
            double a = rnd.NextDouble() * Math.PI * 2;
            float sp = 2 + (float)rnd.NextDouble() * 4;
            sh.X = vx; sh.Y = vy;
            sh.VX = (float)Math.Cos(a) * sp; sh.VY = (float)Math.Sin(a) * sp - 2;
            sh.S = 4 + (float)rnd.NextDouble() * 5;
            sh.L = sh.M = 14 + rnd.Next(8);
            sh.C = (k % 2 == 0) ? lc : dc;
            parts.Add(sh);
        }
        int ti = equipped[5];
        if (ti >= 0)
        {
            Item tl = items[ti];
            gain += tl.Power;
            toolBonus = tl.Bonus;
            swing = 1f;
            shake = Math.Max(shake, Math.Min(6, 1 + tl.Power / 20));
            dur[ti]--;
            if (dur[ti] <= 0) BreakTool(ti);
            else msg = tl.Name + " - HUGG! +" + gain;
        }
        else msg = "AJ! Sluta peta på det!";
        level += gain;
        if (tickN - lastHitSound >= 4) { lastHitSound = tickN; Sound(0); }
    }

    void BreakTool(int ti)
    {
        Item tl = items[ti];
        owned[ti] = false;
        dur[ti] = 0;
        equipped[5] = -1;
        if (curHidden) { Cursor.Show(); curHidden = false; }
        msg = tl.Name + " gick sönder! Köp en ny i butiken.";
        float bx = VX(mx), by = VY(my);
        for (int k = 0; k < 26; k++)
        {
            Part pc = new Part();
            double a = rnd.NextDouble() * Math.PI * 2;
            float sp = 2 + (float)rnd.NextDouble() * 7;
            pc.X = bx; pc.Y = by;
            pc.VX = (float)Math.Cos(a) * sp; pc.VY = (float)Math.Sin(a) * sp - 3;
            pc.S = 5 + (float)rnd.NextDouble() * 8;
            pc.L = pc.M = 25 + rnd.Next(20);
            pc.C = (k % 2 == 0) ? tl.L : tl.D;
            parts.Add(pc);
        }
        shake = Math.Max(shake, 6);
        Sound(4);
        Save(true);
    }

    // ---------- spel ----------
    static Color Hsv(double h, double s, double v)
    {
        h = h % 360; if (h < 0) h += 360;
        double c = v * s, x = c * (1 - Math.Abs((h / 60) % 2 - 1)), m = v - c;
        double rr = 0, gg = 0, bb = 0;
        if (h < 60) { rr = c; gg = x; }
        else if (h < 120) { rr = x; gg = c; }
        else if (h < 180) { gg = c; bb = x; }
        else if (h < 240) { gg = x; bb = c; }
        else if (h < 300) { rr = x; bb = c; }
        else { rr = c; bb = x; }
        return Color.FromArgb((int)((rr + m) * 255), (int)((gg + m) * 255), (int)((bb + m) * 255));
    }

    void Pop()
    {
        int combo = Math.Max(1, Math.Min(5, (int)(level / 100f)));
        int reward = (int)Math.Round((double)(10f * Mult() * (1f + toolBonus / 100f))) * combo;
        toolBonus = 0;
        coins += reward;
        pops++;
        level = 0;
        popTimer = 28;
        shake = 10;
        coinPulse = 1f;
        msg = "PLASK! +" + reward + " äggcoins" + (combo > 1 ? "  COMBO x" + combo + "!" : "");
        Color lc, dc;
        EggColors(out lc, out dc);
        int fxStart = parts.Count;
        for (int i = 0; i < 45; i++)
        {
            Part p = new Part();
            double a = rnd.NextDouble() * Math.PI * 2;
            float sp = 3 + (float)rnd.NextDouble() * 8;
            p.X = 210; p.Y = 330;
            p.VX = (float)Math.Cos(a) * sp;
            p.VY = (float)Math.Sin(a) * sp - 3;
            p.S = 6 + (float)rnd.NextDouble() * 10;
            p.L = p.M = 30 + rnd.Next(25);
            int k = rnd.Next(3);
            p.C = (k == 0) ? lc : ((k == 1) ? dc : Color.FromArgb(255, 200, 0));
            parts.Add(p);
        }
        if (equipped[9] >= 0) ApplyPopFx(items[equipped[9]].Mode, fxStart);
        Floater f = new Floater();
        f.X = 210; f.Y = 250; f.L = 60; f.T = "+" + reward; f.Coin = true;
        floats.Add(f);
        Sound(1);
        Save(true);
    }

    void Tick(object s, EventArgs e)
    {
        tickN++;
        ang += 0.06f;
        if (askName) { Invalidate(); return; }
        if (tickN % 100 == 0 && popTimer == 0) msg = msgs[rnd.Next(msgs.Length)];

        if (popTimer > 0) { popTimer--; if (popTimer == 0) eggScale = 0f; }
        else if (eggScale < 1f) eggScale = Math.Min(1f, eggScale + 0.07f);

        if (popTimer == 0 && eggScale >= 1f)
        {
            if (tickN % 10 == 0) level += 1.0f;
            if (level >= 100f) Pop();
        }
        if (coinPulse > 0) coinPulse = Math.Max(0f, coinPulse - 0.04f);
        if (savedFlash > 0) savedFlash--;
        if (rebirthArm > 0) rebirthArm--;
        if (swing > 0) swing = Math.Max(0f, swing - 0.12f);
        if (tickN % 150 == 0) Save(false);

        if (tickN % 8 == 0)
        {
            Cloud c = new Cloud();
            float x0 = VX(0), x1 = VX(ClientSize.Width);
            c.X = x0 + (float)rnd.NextDouble() * (x1 - x0);
            c.Y = VY(ClientSize.Height) + 20;
            c.S = rnd.Next(25, 60); c.A = 140;
            clouds.Add(c);
        }
        for (int i = clouds.Count - 1; i >= 0; i--)
        {
            clouds[i].Y -= 2; clouds[i].S += 0.3f; clouds[i].A -= 1;
            if (clouds[i].A < 5) clouds.RemoveAt(i);
        }
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            Part p = parts[i];
            p.X += p.VX; p.Y += p.VY; p.VY += 0.35f; p.L--;
            if (p.L <= 0) parts.RemoveAt(i);
        }
        for (int i = floats.Count - 1; i >= 0; i--)
        {
            floats[i].Y -= 1.5f; floats[i].L--;
            if (floats[i].L <= 0) floats.RemoveAt(i);
        }
        if (shake > 0)
        {
            shX = rnd.Next(-(shake * 2 + 1), shake * 2 + 2);
            shY = rnd.Next(-(shake * 2 + 1), shake * 2 + 2);
            shake--;
        }
        else { shX = 0; shY = 0; }
        Invalidate();
    }

    // ---------- rita: hjälpare ----------
    void DrawCoin(Graphics g, float cx, float cy, float r)
    {
        using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 214, 64)))
            g.FillEllipse(b, cx - r, cy - r, 2 * r, 2 * r);
        using (Pen p = new Pen(Color.FromArgb(176, 124, 10), Math.Max(1.5f, r / 7f)))
            g.DrawEllipse(p, cx - r, cy - r, 2 * r, 2 * r);
        using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 250, 225)))
            g.FillEllipse(b, cx - r * 0.38f, cy - r * 0.55f, r * 0.76f, r * 1.1f);
        using (Pen p = new Pen(Color.FromArgb(176, 124, 10), Math.Max(1f, r / 12f)))
            g.DrawEllipse(p, cx - r * 0.38f, cy - r * 0.55f, r * 0.76f, r * 1.1f);
    }

    void DrawHeart(Graphics g, float cx, float cy, float s, Color c)
    {
        using (SolidBrush b = new SolidBrush(c))
        {
            g.FillEllipse(b, cx - s, cy - s * 0.75f, s, s);
            g.FillEllipse(b, cx, cy - s * 0.75f, s, s);
            PointF[] tri = { new PointF(cx - s * 0.98f, cy - s * 0.1f), new PointF(cx + s * 0.98f, cy - s * 0.1f), new PointF(cx, cy + s * 1.1f) };
            g.FillPolygon(b, tri);
        }
    }

    void DrawStar(Graphics g, float cx, float cy, float r, Color c)
    {
        PointF[] pts = new PointF[8];
        for (int i = 0; i < 8; i++)
        {
            double a = -Math.PI / 2 + i * Math.PI / 4;
            float rr = (i % 2 == 0) ? r : r * 0.38f;
            pts[i] = new PointF(cx + (float)Math.Cos(a) * rr, cy + (float)Math.Sin(a) * rr);
        }
        using (SolidBrush b = new SolidBrush(c)) g.FillPolygon(b, pts);
    }

    void DrawPlainEyes(Graphics g, bool left, bool right)
    {
        using (Pen p = new Pen(Color.FromArgb(40, 40, 40), 2))
        {
            if (left)
            {
                g.FillEllipse(Brushes.White, -48, -48, 36, 40); g.DrawEllipse(p, -48, -48, 36, 40);
                g.FillEllipse(Brushes.Black, -37, -35, 14, 16);
            }
            if (right)
            {
                g.FillEllipse(Brushes.White, 12, -48, 36, 40); g.DrawEllipse(p, 12, -48, 36, 40);
                g.FillEllipse(Brushes.Black, 23, -35, 14, 16);
            }
        }
    }

    // ---------- rita: verktyg ----------
    void DrawTool(Graphics g, Item t, float cx, float cy, float sc2, float rot)
    {
        GraphicsState st = g.Save();
        g.TranslateTransform(cx, cy);
        g.RotateTransform(rot);
        g.ScaleTransform(sc2, sc2);
        Color hc = t.L, wc = t.D;
        Color ol = Color.FromArgb(220, hc.R / 3, hc.G / 3, hc.B / 3);
        switch (t.Kind)
        {
            case "spoon":
            {
                using (Pen p = new Pen(wc, 6)) g.DrawLine(p, 0, 12, 0, 72);
                g.FillEllipse(SB(hc), -13, -18, 26, 34);
                using (Pen o = new Pen(ol, 2)) g.DrawEllipse(o, -13, -18, 26, 34);
                break;
            }
            case "fork":
            {
                using (Pen p = new Pen(wc, 6)) g.DrawLine(p, 0, 12, 0, 72);
                g.FillRectangle(SB(hc), -11, 0, 22, 9);
                for (int k = 0; k < 3; k++) g.FillRectangle(SB(hc), -11 + k * 9, -26, 4, 28);
                break;
            }
            case "pick":
            {
                using (Pen p = new Pen(wc, 7)) g.DrawLine(p, 0, -6, 0, 74);
                using (Pen hp = new Pen(hc, 9))
                {
                    hp.StartCap = LineCap.Triangle;
                    hp.EndCap = LineCap.Triangle;
                    g.DrawArc(hp, -36, -28, 72, 52, 195, 150);
                }
                g.FillRectangle(SB(hc), -6, -8, 12, 14);
                using (Pen o = new Pen(ol, 2)) g.DrawRectangle(o, -6, -8, 12, 14);
                break;
            }
            case "pan":
            {
                using (Pen p = new Pen(wc, 7)) g.DrawLine(p, 0, 16, 0, 72);
                g.FillEllipse(SB(hc), -26, -40, 52, 56);
                using (Pen o = new Pen(ol, 3))
                {
                    g.DrawEllipse(o, -26, -40, 52, 56);
                    g.DrawEllipse(o, -18, -32, 36, 40);
                }
                break;
            }
            case "hammer":
            {
                using (Pen p = new Pen(wc, 7)) g.DrawLine(p, 0, -10, 0, 74);
                g.FillRectangle(SB(hc), -24, -26, 48, 24);
                g.FillRectangle(SB(Color.FromArgb(120, 255, 255, 255)), -24, -26, 48, 6);
                using (Pen o = new Pen(ol, 2)) g.DrawRectangle(o, -24, -26, 48, 24);
                break;
            }
            case "axe":
            {
                using (Pen p = new Pen(wc, 7)) g.DrawLine(p, 0, -30, 0, 74);
                PointF[] bl = { new PointF(4, -34), new PointF(36, -44), new PointF(36, 8), new PointF(4, -6) };
                g.FillPolygon(SB(hc), bl);
                using (Pen o = new Pen(ol, 2)) g.DrawPolygon(o, bl);
                using (Pen e = new Pen(Color.FromArgb(200, 255, 255, 255), 2)) g.DrawLine(e, 36, -44, 36, 8);
                break;
            }
            case "bat":
            {
                PointF[] bp = { new PointF(-4, 72), new PointF(4, 72), new PointF(8, 0), new PointF(10, -36), new PointF(-10, -36), new PointF(-8, 0) };
                g.FillPolygon(SB(hc), bp);
                using (Pen o = new Pen(ol, 2)) g.DrawPolygon(o, bp);
                g.FillRectangle(SB(wc), -4, 48, 8, 24);
                break;
            }
            case "sword":
            {
                g.FillRectangle(SB(wc), -4, 12, 8, 34);
                g.FillRectangle(SB(Color.FromArgb(255, 200, 170, 60)), -18, 8, 36, 7);
                PointF[] bl = { new PointF(-6, 8), new PointF(6, 8), new PointF(6, -62), new PointF(0, -76), new PointF(-6, -62) };
                g.FillPolygon(SB(hc), bl);
                using (Pen o = new Pen(ol, 2)) g.DrawPolygon(o, bl);
                using (Pen c = new Pen(Color.FromArgb(150, 140, 150, 170), 1)) g.DrawLine(c, 0, 6, 0, -66);
                break;
            }
            case "chainsaw":
            {
                g.FillRectangle(SB(hc), -16, 4, 32, 46);
                using (Pen hp = new Pen(wc, 5)) g.DrawArc(hp, 6, 14, 24, 40, 270, 180);
                g.FillRectangle(SB(RGB(150, 155, 165)), -7, -66, 14, 72);
                using (Pen tp = new Pen(RGB(40, 40, 45), 3))
                {
                    tp.DashStyle = DashStyle.Dash;
                    tp.DashOffset = tickN % 8;
                    g.DrawRectangle(tp, -7, -66, 14, 72);
                }
                g.FillEllipse(SB(RGB(40, 40, 45)), -6, 18, 12, 12);
                break;
            }
            case "wand":
            {
                using (Pen p = new Pen(wc, 6)) g.DrawLine(p, 0, -6, 0, 74);
                g.FillEllipse(SB(Color.FromArgb(70, 255, 230, 120)), -26, -50, 52, 52);
                DrawStar(g, 0, -24, 16, hc);
                for (int k = 0; k < 4; k++)
                {
                    float a = ang * 3 + k * 1.57f;
                    g.FillEllipse(SB(Color.FromArgb(200, 255, 255, 200)), (float)Math.Cos(a) * 26 - 2, -24 + (float)Math.Sin(a) * 26 - 2, 4, 4);
                }
                break;
            }
            case "saber":
            {
                g.FillRectangle(SB(wc), -5, 12, 10, 46);
                g.FillRectangle(SB(RGB(60, 60, 70)), -6, 22, 12, 5);
                g.FillRectangle(SB(RGB(60, 60, 70)), -6, 40, 12, 5);
                g.FillRectangle(SB(Color.FromArgb(60, hc)), -12, -78, 24, 92);
                g.FillRectangle(SB(Color.FromArgb(120, hc)), -8, -76, 16, 88);
                g.FillRectangle(SB(Color.White), -3, -74, 6, 86);
                break;
            }
            case "baguette":
            {
                g.FillEllipse(SB(hc), -11, -40, 22, 116);
                using (Pen o = new Pen(ol, 2)) g.DrawEllipse(o, -11, -40, 22, 116);
                using (Pen sl = new Pen(Color.FromArgb(150, 150, 90, 30), 3))
                    for (int k = 0; k < 5; k++) g.DrawLine(sl, -6, -26 + k * 20, 6, -16 + k * 20);
                break;
            }
            case "scythe":
            {
                using (Pen p = new Pen(wc, 6)) g.DrawLine(p, 0, -40, 0, 74);
                PointF[] bl = { new PointF(0, -72), new PointF(46, -66), new PointF(64, -38), new PointF(40, -52), new PointF(0, -54) };
                g.FillPolygon(SB(hc), bl);
                using (Pen o = new Pen(ol, 2)) g.DrawPolygon(o, bl);
                break;
            }
            case "trident":
            {
                using (Pen p = new Pen(wc, 6)) g.DrawLine(p, 0, -8, 0, 74);
                g.FillRectangle(SB(hc), -16, -12, 32, 6);
                using (Pen hp = new Pen(hc, 5))
                {
                    g.DrawLine(hp, -14, -10, -14, -44);
                    g.DrawLine(hp, 0, -10, 0, -56);
                    g.DrawLine(hp, 14, -10, 14, -44);
                }
                g.FillPolygon(SB(hc), new PointF[] { new PointF(-18, -44), new PointF(-10, -44), new PointF(-14, -54) });
                g.FillPolygon(SB(hc), new PointF[] { new PointF(-4, -56), new PointF(4, -56), new PointF(0, -68) });
                g.FillPolygon(SB(hc), new PointF[] { new PointF(10, -44), new PointF(18, -44), new PointF(14, -54) });
                break;
            }
            case "guitar":
            {
                using (Pen p = new Pen(wc, 8)) g.DrawLine(p, 0, -64, 0, 6);
                g.FillRectangle(SB(RGB(40, 30, 25)), -6, -78, 12, 16);
                g.FillEllipse(SB(hc), -20, 0, 40, 34);
                g.FillEllipse(SB(hc), -26, 26, 52, 46);
                g.FillEllipse(SB(RGB(30, 20, 15)), -9, 26, 18, 18);
                using (Pen sp = new Pen(Color.FromArgb(200, 230, 230, 230), 1)) { g.DrawLine(sp, -2, -62, -2, 60); g.DrawLine(sp, 2, -62, 2, 60); }
                break;
            }
            default:
            {
                using (Pen p = new Pen(wc, 6)) g.DrawLine(p, 0, -20, 0, 70);
                break;
            }
        }
        g.Restore(st);
    }

    // ================= version 11: massor av nytt =================
    void DrawPattern(Graphics g, int pat, Color pc)
    {
        if (pat <= 0) return;
        GraphicsPath gp = new GraphicsPath();
        gp.AddEllipse(-80, -110, 160, 210);
        GraphicsState st = g.Save();
        g.SetClip(gp);
        Color c = Color.FromArgb(215, pc);
        SolidBrush b = SB(c);
        if (pat == 1)
        {
            for (int y = -110; y < 100; y += 24) g.FillRectangle(b, -90, y, 180, 12);
        }
        else if (pat == 2)
        {
            for (int yy = 0; yy < 8; yy++)
                for (int xx = 0; xx < 7; xx++)
                    g.FillEllipse(b, -84 + xx * 26 + (yy % 2) * 13, -108 + yy * 28, 13, 13);
        }
        else if (pat == 3)
        {
            for (int yy = 0; yy < 11; yy++)
                for (int xx = 0; xx < 8; xx++)
                    if ((xx + yy) % 2 == 0) g.FillRectangle(b, -84 + xx * 22, -110 + yy * 22, 22, 22);
        }
        else if (pat == 4)
        {
            for (int x = -90; x < 90; x += 24) g.FillRectangle(b, x, -120, 11, 230);
        }
        else if (pat == 5)
        {
            using (Pen zp = new Pen(c, 5))
                for (int row = 0; row < 7; row++)
                {
                    PointF[] zz = new PointF[10];
                    for (int j = 0; j < 10; j++) zz[j] = new PointF(-90 + j * 20, -100 + row * 32 + ((j % 2 == 0) ? 0 : 12));
                    g.DrawLines(zp, zz);
                }
        }
        else if (pat == 6)
        {
            for (int yy = 0; yy < 6; yy++)
                for (int xx = 0; xx < 5; xx++)
                    DrawHeart(g, -64 + xx * 34 + (yy % 2) * 17, -86 + yy * 34, 8, c);
        }
        else if (pat == 7)
        {
            for (int yy = 0; yy < 6; yy++)
                for (int xx = 0; xx < 5; xx++)
                    DrawStar(g, -64 + xx * 34 + (yy % 2) * 17, -86 + yy * 34, 9, c);
        }
        else if (pat == 8)
        {
            for (int k = 0; k < 12; k++)
                g.FillEllipse(b, ((k * 47) % 160) - 80, ((k * 71) % 200) - 105, 30 + (k % 3) * 12, 22 + (k % 4) * 6);
        }
        else if (pat == 9)
        {
            using (Pen dp = new Pen(c, 10))
                for (int x = -200; x < 200; x += 28) g.DrawLine(dp, x, 110, x + 140, -120);
        }
        g.Restore(st);
        gp.Dispose();
    }

    void ApplyPopFx(int mode, int from)
    {
        int n = parts.Count - from;
        for (int i = from; i < parts.Count; i++)
        {
            Part q = parts[i];
            int idx = i - from;
            if (mode == 1) { q.Sh = 1; q.C = Hsv(rnd.Next(360), 0.85, 1.0); q.S = 6 + rnd.Next(6); }
            else if (mode == 2) { q.Sh = 0; q.C = (idx % 2 == 0) ? Color.FromArgb(255, 215, 60) : Color.FromArgb(255, 240, 150); q.S += 4; }
            else if (mode == 3) { q.Sh = 3; q.C = (idx % 2 == 0) ? Color.FromArgb(255, 70, 120) : Color.FromArgb(255, 150, 190); q.S = 6 + rnd.Next(5); }
            else if (mode == 4)
            {
                double a = idx * 2 * Math.PI / Math.Max(1, n);
                float sp = (idx % 3 + 1) * 3.2f;
                q.VX = (float)Math.Cos(a) * sp; q.VY = (float)Math.Sin(a) * sp;
                q.Sh = 0; q.C = Hsv((idx % 3) * 110 + 20, 0.9, 1.0); q.S = 5; q.L = 40; q.M = 40;
            }
            else if (mode == 5) { q.Sh = 0; q.C = Color.FromArgb(240, 250, 255); q.S = 3 + rnd.Next(4); q.VY = Math.Abs(q.VY) * 0.4f; }
            else if (mode == 6) { q.Sh = 0; q.C = Hsv(rnd.Next(10, 55), 1.0, 1.0); q.VY = -Math.Abs(q.VY) - 2; q.S += 3; }
            else if (mode == 7) { q.Sh = 0; q.C = Hsv(idx * 8, 0.9, 1.0); q.S += 2; }
            else if (mode == 8) { q.Sh = 2; q.C = (idx % 2 == 0) ? Color.FromArgb(255, 235, 100) : Color.White; q.S = 8 + rnd.Next(6); }
            else if (mode == 9) { q.Sh = 4; q.C = Color.FromArgb(180, 220, 255); q.S = 8 + rnd.Next(12); }
        }
    }

    void DrawBackground(Graphics g)
    {
        if (equipped[8] < 0) return;
        Item it = items[equipped[8]];
        int W = ClientSize.Width, H = ClientSize.Height;
        if (W < 2 || H < 2) return;
        using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(0, 0, W, H), it.L, it.D, 90f))
            g.FillRectangle(lb, 0, 0, W, H);
        int m = it.Mode;
        if (m == 1)
        {
            for (int k = 0; k < 80; k++)
            {
                int x = (k * 131) % W, y = (k * 277) % (H * 3 / 4);
                int al = (int)(130 + 120 * Math.Sin(ang * 3 + k));
                g.FillEllipse(SB(Color.FromArgb(al, 255, 255, 230)), x, y, 2 + k % 3, 2 + k % 3);
            }
        }
        else if (m == 2)
        {
            g.FillEllipse(SB(RGB(255, 235, 120)), W * 0.78f, H * 0.1f, 110, 110);
            g.FillEllipse(SB(RGB(70, 150, 60)), -W * 0.2f, H * 0.78f, W * 0.8f, H * 0.7f);
            g.FillEllipse(SB(RGB(95, 180, 80)), W * 0.3f, H * 0.86f, W * 0.9f, H * 0.6f);
        }
        else if (m == 3)
        {
            for (int layer = 0; layer < 3; layer++)
            {
                PointF[] wv = new PointF[42];
                for (int j = 0; j < 40; j++)
                {
                    float x = j * (W / 39f);
                    wv[j] = new PointF(x, H * (0.78f + layer * 0.07f) + (float)Math.Sin(ang * 2 + j * 0.5 + layer) * 14);
                }
                wv[40] = new PointF(W, H);
                wv[41] = new PointF(0, H);
                g.FillPolygon(SB(Color.FromArgb(120, 20 + layer * 20, 90 + layer * 30, 190)), wv);
            }
        }
        else if (m == 4)
        {
            for (int k = 0; k < 90; k++)
            {
                float x = (k * 97) % W + (float)Math.Sin(ang + k) * 10;
                float y = ((ang * 60 + k * 53) % (H + 20)) - 10;
                g.FillEllipse(SB(Color.FromArgb(220, 255, 255, 255)), x, y, 4, 4);
            }
            g.FillRectangle(SB(RGB(235, 245, 255)), 0, H * 0.92f, W, H * 0.08f + 1);
        }
        else if (m == 5)
        {
            g.FillEllipse(SB(Color.FromArgb(90, 255, 90, 0)), -W * 0.2f, H * 0.7f, W * 1.4f, H * 0.7f);
            for (int k = 0; k < 40; k++)
            {
                float x = (k * 151) % W;
                float y = H - ((ang * 70 + k * 41) % H);
                g.FillEllipse(SB(Color.FromArgb(200, 255, 170 + (k % 5) * 15, 40)), x, y, 4, 4);
            }
        }
        else if (m == 6)
        {
            int bn = 16;
            for (int k = 0; k < bn; k++)
            {
                float bw = W / (float)bn;
                float bh = (80 + ((k * 53) % 160)) * (H / 1080f) * 2f;
                g.FillRectangle(SB(RGB(25, 25, 45)), k * bw, H - bh, bw + 1, bh);
                for (int wy = 0; wy < (int)(bh / 22); wy++)
                    if ((k * 7 + wy * 3) % 4 != 0) g.FillRectangle(SB(RGB(255, 225, 120)), k * bw + 6, H - bh + 10 + wy * 22, 6, 8);
            }
        }
        else if (m == 7)
        {
            float hy = H * 0.55f;
            g.FillEllipse(SB(RGB(255, 120, 190)), W / 2f - 120, hy - 120, 240, 240);
            g.FillRectangle(SB(RGB(40, 10, 70)), 0, hy, W, H - hy);
            using (Pen gp2 = new Pen(Color.FromArgb(200, 255, 60, 220), 2))
            {
                for (int k = -12; k <= 12; k++) g.DrawLine(gp2, W / 2f + k * 20, hy, W / 2f + k * W / 12f, H);
                for (int k = 1; k < 10; k++)
                {
                    float yy = hy + (float)Math.Pow(k / 10f + (ang * 0.2f) % 0.1f, 2) * (H - hy);
                    g.DrawLine(gp2, 0, yy, W, yy);
                }
            }
        }
        else if (m == 8)
        {
            for (int k = 0; k < 6; k++)
            {
                float x = ((ang * 25 + k * (W / 5f)) % (W + 400f)) - 200;
                float y = H * 0.08f + k * 46;
                g.FillEllipse(SB(Color.FromArgb(200, 255, 255, 255)), x, y, 180, 50);
                g.FillEllipse(SB(Color.FromArgb(200, 255, 255, 255)), x + 40, y - 20, 110, 50);
            }
        }
        else if (m == 9)
        {
            for (int k = 0; k < 40; k++)
            {
                float x = (k * 113) % W + (float)Math.Sin(ang * 2 + k) * 8;
                float y = H - ((ang * 50 + k * 67) % (H + 30));
                float r = 3 + k % 5 * 2;
                using (Pen bp = new Pen(Color.FromArgb(150, 220, 255, 255), 2)) g.DrawEllipse(bp, x, y, r * 2, r * 2);
            }
        }
        else if (m == 10)
        {
            g.FillEllipse(SB(RGB(255, 245, 190)), W * 0.7f, H * 0.12f, 130, 130);
            g.FillEllipse(SB(RGB(220, 170, 90)), -W * 0.1f, H * 0.82f, W * 0.7f, H * 0.6f);
            g.FillEllipse(SB(RGB(235, 190, 110)), W * 0.35f, H * 0.88f, W * 0.8f, H * 0.5f);
        }
    }

    void DrawPet(Graphics g)
    {
        if (equipped[7] < 0) return;
        string nm = items[equipped[7]].Name;
        bool flyer = (nm == "Bi" || nm == "Spöke" || nm == "Drönare" || nm == "Drake" || nm == "Rymdvarelse" || nm == "Fisk");
        float px, py, dir;
        if (flyer)
        {
            double fa = ang * 0.9;
            px = 210 + (float)Math.Cos(fa) * 150;
            py = 300 + (float)Math.Sin(fa * 1.3) * 80 + (float)Math.Sin(ang * 5) * 5;
            dir = (Math.Sin(fa) > 0) ? -1f : 1f;
        }
        else
        {
            px = 210 + (float)Math.Sin(ang * 0.6) * 140;
            py = 470 - (float)Math.Abs(Math.Sin(ang * 6)) * 7;
            dir = (Math.Cos(ang * 0.6) > 0) ? 1f : -1f;
        }
        GraphicsState st = g.Save();
        g.TranslateTransform(px, py);
        g.ScaleTransform(dir, 1f);
        if (nm == "Kyckling")
        {
            using (Pen lp = new Pen(RGB(240, 150, 30), 3)) { g.DrawLine(lp, -4, -4, -4, 4); g.DrawLine(lp, 5, -4, 5, 4); }
            g.FillEllipse(SB(RGB(255, 225, 70)), -16, -30, 32, 28);
            g.FillEllipse(SB(RGB(255, 225, 70)), 4, -46, 20, 20);
            g.FillPolygon(SB(RGB(240, 150, 30)), new PointF[] { new PointF(23, -39), new PointF(31, -36), new PointF(23, -33) });
            g.FillEllipse(Brushes.Black, 15, -40, 4, 4);
            g.FillEllipse(SB(RGB(240, 190, 40)), -14, -24, 14, 12);
        }
        else if (nm == "Katt")
        {
            using (Pen tp = new Pen(RGB(120, 120, 130), 5))
            {
                tp.StartCap = LineCap.Round; tp.EndCap = LineCap.Round;
                g.DrawCurve(tp, new PointF[] { new PointF(-16, -14), new PointF(-30, -24), new PointF(-26, -40) });
            }
            g.FillEllipse(SB(RGB(130, 130, 140)), -18, -28, 38, 24);
            g.FillEllipse(SB(RGB(130, 130, 140)), 10, -48, 24, 22);
            g.FillPolygon(SB(RGB(130, 130, 140)), new PointF[] { new PointF(12, -46), new PointF(15, -58), new PointF(21, -47) });
            g.FillPolygon(SB(RGB(130, 130, 140)), new PointF[] { new PointF(24, -47), new PointF(29, -58), new PointF(33, -46) });
            g.FillEllipse(Brushes.LimeGreen, 18, -40, 4, 5);
            g.FillEllipse(Brushes.LimeGreen, 26, -40, 4, 5);
            g.FillRectangle(SB(RGB(110, 110, 120)), -12, -6, 5, 6);
            g.FillRectangle(SB(RGB(110, 110, 120)), 12, -6, 5, 6);
        }
        else if (nm == "Pingvin")
        {
            g.FillEllipse(SB(RGB(240, 150, 30)), -12, -4, 11, 6);
            g.FillEllipse(SB(RGB(240, 150, 30)), 2, -4, 11, 6);
            g.FillEllipse(SB(RGB(30, 30, 40)), -15, -44, 30, 42);
            g.FillEllipse(Brushes.White, -10, -34, 20, 30);
            g.FillPolygon(SB(RGB(240, 150, 30)), new PointF[] { new PointF(4, -34), new PointF(14, -31), new PointF(4, -28) });
            g.FillEllipse(Brushes.White, 0, -39, 5, 5);
            g.FillEllipse(Brushes.Black, 2, -38, 2, 3);
        }
        else if (nm == "Uggla")
        {
            g.FillEllipse(SB(RGB(130, 90, 50)), -16, -42, 32, 40);
            g.FillEllipse(SB(RGB(190, 150, 100)), -10, -26, 20, 22);
            g.FillEllipse(Brushes.White, -13, -36, 12, 12);
            g.FillEllipse(Brushes.White, 1, -36, 12, 12);
            g.FillEllipse(Brushes.Black, -9, -33, 5, 6);
            g.FillEllipse(Brushes.Black, 5, -33, 5, 6);
            g.FillPolygon(SB(RGB(240, 170, 30)), new PointF[] { new PointF(-3, -27), new PointF(3, -27), new PointF(0, -21) });
            g.FillPolygon(SB(RGB(110, 70, 40)), new PointF[] { new PointF(-16, -40), new PointF(-10, -48), new PointF(-6, -40) });
            g.FillPolygon(SB(RGB(110, 70, 40)), new PointF[] { new PointF(16, -40), new PointF(10, -48), new PointF(6, -40) });
        }
        else if (nm == "Bi")
        {
            float wf = 4 + (float)Math.Abs(Math.Sin(ang * 30)) * 9;
            g.FillEllipse(SB(Color.FromArgb(200, 235, 245, 255)), -8, -8 - wf, 10, wf + 4);
            g.FillEllipse(SB(Color.FromArgb(200, 235, 245, 255)), 0, -8 - wf, 10, wf + 4);
            g.FillEllipse(SB(RGB(255, 210, 40)), -14, -8, 28, 18);
            g.FillRectangle(Brushes.Black, -4, -7, 4, 16);
            g.FillRectangle(Brushes.Black, 4, -5, 4, 12);
            g.FillEllipse(Brushes.Black, 8, -4, 3, 3);
            g.FillPolygon(Brushes.Black, new PointF[] { new PointF(-14, 0), new PointF(-20, 2), new PointF(-14, 4) });
        }
        else if (nm == "Spöke")
        {
            g.FillEllipse(SB(Color.FromArgb(215, 255, 255, 255)), -16, -24, 32, 28);
            g.FillRectangle(SB(Color.FromArgb(215, 255, 255, 255)), -16, -10, 32, 18);
            for (int k = 0; k < 3; k++) g.FillEllipse(SB(Color.FromArgb(215, 255, 255, 255)), -16 + k * 11, 2, 11, 12);
            g.FillEllipse(Brushes.Black, -8, -14, 5, 7);
            g.FillEllipse(Brushes.Black, 3, -14, 5, 7);
        }
        else if (nm == "Fisk")
        {
            float tw = (float)Math.Sin(ang * 12) * 5;
            g.FillPolygon(SB(RGB(240, 120, 30)), new PointF[] { new PointF(-14, 0), new PointF(-30, -10 + tw), new PointF(-30, 10 + tw) });
            g.FillEllipse(SB(RGB(255, 150, 40)), -18, -10, 36, 20);
            g.FillEllipse(Brushes.White, 8, -5, 7, 7);
            g.FillEllipse(Brushes.Black, 11, -3, 3, 3);
            g.FillPolygon(SB(RGB(240, 120, 30)), new PointF[] { new PointF(-4, -9), new PointF(4, -16), new PointF(6, -9) });
        }
        else if (nm == "Drönare")
        {
            g.FillRectangle(SB(RGB(60, 62, 72)), -14, -5, 28, 12);
            g.FillEllipse(SB(RGB(30, 30, 36)), 4, 6, 10, 8);
            float ps = Math.Max(0.1f, Math.Abs((float)Math.Cos(ang * 30)));
            using (Pen rp = new Pen(RGB(190, 190, 200), 3))
            {
                g.DrawLine(rp, -14 - 10 * ps, -9, -14 + 10 * ps, -9);
                g.DrawLine(rp, 14 - 10 * ps, -9, 14 + 10 * ps, -9);
            }
            if (Math.Sin(ang * 8) > 0) g.FillEllipse(Brushes.Red, -2, -3, 5, 5);
        }
        else if (nm == "Rymdvarelse")
        {
            using (Pen ap = new Pen(RGB(90, 190, 90), 2)) { g.DrawLine(ap, -6, -16, -10, -28); g.DrawLine(ap, 6, -16, 10, -28); }
            g.FillEllipse(Brushes.Yellow, -13, -32, 6, 6);
            g.FillEllipse(Brushes.Yellow, 7, -32, 6, 6);
            g.FillEllipse(SB(RGB(110, 210, 110)), -14, -18, 28, 26);
            g.FillEllipse(Brushes.Black, -10, -9, 9, 6);
            g.FillEllipse(Brushes.Black, 2, -9, 9, 6);
            g.FillEllipse(SB(RGB(90, 190, 90)), -8, 6, 16, 12);
        }
        else if (nm == "Drake")
        {
            float fw = (float)Math.Sin(ang * 10) * 10;
            g.FillPolygon(SB(RGB(60, 150, 70)), new PointF[] { new PointF(-4, -6), new PointF(-22, -26 - fw), new PointF(4, -14) });
            using (Pen dp = new Pen(RGB(60, 150, 70), 4))
            {
                dp.StartCap = LineCap.Round; dp.EndCap = LineCap.Round;
                g.DrawCurve(dp, new PointF[] { new PointF(-14, 2), new PointF(-30, 8), new PointF(-36, -2) });
            }
            g.FillEllipse(SB(RGB(70, 170, 80)), -16, -8, 32, 20);
            g.FillEllipse(SB(RGB(70, 170, 80)), 10, -16, 18, 16);
            g.FillEllipse(Brushes.Yellow, 20, -12, 4, 4);
            if (Math.Sin(ang * 6) > 0.3) g.FillEllipse(SB(RGB(255, 150, 30)), 28, -10, 9, 7);
        }
        g.Restore(st);
    }

    void DrawBodyBack(Graphics g)
    {
        if (equipped[6] < 0) return;
        string nm = items[equipped[6]].Name;
        if (nm == "Cape")
        {
            float wv = (float)Math.Sin(ang * 3) * 8;
            PointF[] cp = { new PointF(-60, -60), new PointF(60, -60), new PointF(100 + wv, 150), new PointF(-100 - wv, 150) };
            g.FillPolygon(SB(RGB(190, 25, 45)), cp);
            using (Pen op = new Pen(RGB(120, 10, 25), 3)) g.DrawPolygon(op, cp);
        }
        else if (nm == "Änglavingar")
        {
            float fl = 0.75f + 0.25f * (float)Math.Sin(ang * 6);
            for (int sd = -1; sd <= 1; sd += 2)
                for (int k = 0; k < 4; k++)
                {
                    float len = (110 - k * 14) * fl;
                    PointF[] wg = { new PointF(sd * 60, -40 + k * 22), new PointF(sd * (60 + len), -90 + k * 26), new PointF(sd * (60 + len * 0.8f), -50 + k * 26), new PointF(sd * 58, -10 + k * 22) };
                    g.FillPolygon(SB(Color.FromArgb(235, 255, 255, 255)), wg);
                    using (Pen op = new Pen(RGB(200, 210, 230), 2)) g.DrawPolygon(op, wg);
                }
        }
        else if (nm == "Fjärilsvingar")
        {
            float fl = 0.7f + 0.3f * (float)Math.Sin(ang * 5);
            for (int sd = -1; sd <= 1; sd += 2)
            {
                g.FillEllipse(SB(RGB(255, 140, 40)), sd > 0 ? 40 : -40 - 70 * fl, -80, 70 * fl, 70);
                g.FillEllipse(SB(RGB(60, 120, 230)), sd > 0 ? 40 : -40 - 55 * fl, 0, 55 * fl, 60);
            }
        }
        else if (nm == "Drakvingar")
        {
            float fl = 0.8f + 0.2f * (float)Math.Sin(ang * 4);
            for (int sd = -1; sd <= 1; sd += 2)
            {
                PointF[] wg = { new PointF(sd * 56, -50), new PointF(sd * 150 * fl, -140), new PointF(sd * 130 * fl, -70), new PointF(sd * 160 * fl, -40), new PointF(sd * 120 * fl, -10), new PointF(sd * 90 * fl, 40), new PointF(sd * 56, 20) };
                g.FillPolygon(SB(RGB(130, 20, 40)), wg);
                using (Pen op = new Pen(RGB(70, 5, 20), 3))
                {
                    g.DrawPolygon(op, wg);
                    g.DrawLine(op, sd * 56, -40, sd * 140 * fl, -120);
                    g.DrawLine(op, sd * 56, -30, sd * 150 * fl, -40);
                }
            }
        }
        else if (nm == "Jetpack")
        {
            for (int sd = -1; sd <= 1; sd += 2)
            {
                g.FillRectangle(SB(RGB(120, 125, 135)), sd > 0 ? 60 : -80, -50, 20, 90);
                g.FillRectangle(SB(RGB(200, 40, 40)), sd > 0 ? 60 : -80, -50, 20, 14);
                float fh = 30 + (float)Math.Abs(Math.Sin(ang * 25 + sd)) * 28;
                PointF[] fa = { new PointF(sd * 70 - 9, 40), new PointF(sd * 70 + 9, 40), new PointF(sd * 70, 40 + fh) };
                PointF[] fb = { new PointF(sd * 70 - 5, 40), new PointF(sd * 70 + 5, 40), new PointF(sd * 70, 40 + fh * 0.6f) };
                g.FillPolygon(SB(RGB(255, 150, 20)), fa);
                g.FillPolygon(SB(RGB(255, 240, 100)), fb);
            }
        }
    }

    void DrawBodyFront(Graphics g)
    {
        if (equipped[6] < 0) return;
        string nm = items[equipped[6]].Name;
        if (nm == "Tröja")
        {
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(-80, -110, 160, 210);
            GraphicsState st = g.Save();
            g.SetClip(gp);
            g.FillRectangle(SB(RGB(50, 110, 220)), -90, 20, 180, 90);
            for (int k = 0; k < 3; k++) g.FillRectangle(SB(RGB(240, 240, 250)), -90, 34 + k * 24, 180, 8);
            g.Restore(st);
            gp.Dispose();
        }
        else if (nm == "Fluga")
        {
            PointF[] lt = { new PointF(0, 46), new PointF(-30, 30), new PointF(-30, 62) };
            PointF[] rt = { new PointF(0, 46), new PointF(30, 30), new PointF(30, 62) };
            g.FillPolygon(SB(RGB(210, 30, 60)), lt);
            g.FillPolygon(SB(RGB(210, 30, 60)), rt);
            g.FillEllipse(SB(RGB(150, 10, 40)), -7, 39, 14, 14);
        }
        else if (nm == "Halsduk")
        {
            using (Pen sp = new Pen(RGB(40, 150, 90), 16))
            {
                sp.StartCap = LineCap.Round; sp.EndCap = LineCap.Round;
                g.DrawCurve(sp, new PointF[] { new PointF(-62, 28), new PointF(-30, 44), new PointF(30, 44), new PointF(62, 28) });
            }
            float sw = (float)Math.Sin(ang * 3) * 4;
            g.FillRectangle(SB(RGB(40, 150, 90)), 20 + sw, 44, 18, 56);
            g.FillRectangle(SB(RGB(240, 240, 240)), 20 + sw, 62, 18, 6);
            g.FillRectangle(SB(RGB(240, 240, 240)), 20 + sw, 80, 18, 6);
        }
        else if (nm == "Rustning")
        {
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(-80, -110, 160, 210);
            GraphicsState st = g.Save();
            g.SetClip(gp);
            g.FillRectangle(SB(RGB(150, 155, 170)), -90, 0, 180, 110);
            using (Pen rp = new Pen(RGB(90, 95, 110), 3))
            {
                g.DrawLine(rp, -90, 0, 90, 0);
                g.DrawLine(rp, 0, 0, 0, 110);
                g.DrawLine(rp, -90, 44, 90, 44);
            }
            for (int k = 0; k < 4; k++)
            {
                g.FillEllipse(SB(RGB(210, 215, 225)), -64 + k * 38, 10, 8, 8);
                g.FillEllipse(SB(RGB(210, 215, 225)), -64 + k * 38, 58, 8, 8);
            }
            g.Restore(st);
            gp.Dispose();
        }
        else if (nm == "Tutu")
        {
            for (int k = 0; k < 3; k++)
                g.FillEllipse(SB(Color.FromArgb(230, 255, 160 + k * 20, 200 + k * 15)), -78 + k * 6, 66 + k * 8, 156 - k * 12, 26);
        }
        else if (nm == "Smoking")
        {
            PointF[] sh = { new PointF(-22, 8), new PointF(22, 8), new PointF(0, 70) };
            g.FillPolygon(Brushes.White, sh);
            PointF[] ll = { new PointF(-52, 2), new PointF(-22, 8), new PointF(0, 70), new PointF(-30, 96), new PointF(-70, 60) };
            PointF[] lr = { new PointF(52, 2), new PointF(22, 8), new PointF(0, 70), new PointF(30, 96), new PointF(70, 60) };
            g.FillPolygon(SB(RGB(25, 25, 30)), ll);
            g.FillPolygon(SB(RGB(25, 25, 30)), lr);
            PointF[] bl = { new PointF(0, 14), new PointF(-14, 6), new PointF(-14, 22) };
            PointF[] br = { new PointF(0, 14), new PointF(14, 6), new PointF(14, 22) };
            g.FillPolygon(SB(RGB(200, 20, 40)), bl);
            g.FillPolygon(SB(RGB(200, 20, 40)), br);
        }
        else if (nm == "Armar")
        {
            float wv = (float)Math.Sin(ang * 6) * 25;
            using (Pen ap = new Pen(RGB(60, 40, 20), 6))
            {
                ap.StartCap = LineCap.Round; ap.EndCap = LineCap.Round;
                g.DrawLine(ap, -74, 10, -112, 40 - wv);
                g.DrawLine(ap, 74, 10, 112, 40 + wv);
            }
            g.FillEllipse(SB(RGB(255, 225, 190)), -122, 32 - wv, 16, 16);
            g.FillEllipse(SB(RGB(255, 225, 190)), 106, 32 + wv, 16, 16);
        }
        else if (nm == "Gångben")
        {
            float st1 = (float)Math.Sin(ang * 7) * 14;
            using (Pen lp = new Pen(RGB(60, 40, 20), 6))
            {
                lp.StartCap = LineCap.Round; lp.EndCap = LineCap.Round;
                g.DrawLine(lp, -24, 96, -24 + st1, 148);
                g.DrawLine(lp, 24, 96, 24 - st1, 148);
            }
            g.FillEllipse(SB(RGB(40, 40, 50)), -24 + st1 - 14, 144, 26, 12);
            g.FillEllipse(SB(RGB(40, 40, 50)), 24 - st1 - 12, 144, 26, 12);
        }
        else if (nm == "Hängslen")
        {
            using (Pen hp = new Pen(RGB(200, 40, 40), 7))
            {
                g.DrawLine(hp, -30, -10, -26, 92);
                g.DrawLine(hp, 30, -10, 26, 92);
            }
            g.FillRectangle(SB(RGB(230, 200, 60)), -34, 8, 8, 8);
            g.FillRectangle(SB(RGB(230, 200, 60)), 26, 8, 8, 8);
        }
    }

    bool DrawHead2(Graphics g, string nm)
    {
        if (nm == "Viking")
        {
            g.FillPie(SB(RGB(150, 155, 165)), -42, -134, 84, 70, 180, 180);
            g.FillRectangle(SB(RGB(100, 105, 115)), -43, -102, 86, 8);
            PointF[] hl = { new PointF(-40, -104), new PointF(-62, -116), new PointF(-72, -146), new PointF(-56, -126), new PointF(-42, -114) };
            PointF[] hr = { new PointF(40, -104), new PointF(62, -116), new PointF(72, -146), new PointF(56, -126), new PointF(42, -114) };
            g.FillPolygon(SB(RGB(240, 230, 200)), hl);
            g.FillPolygon(SB(RGB(240, 230, 200)), hr);
            return true;
        }
        if (nm == "Sombrero")
        {
            g.FillEllipse(SB(RGB(215, 170, 90)), -76, -110, 152, 26);
            PointF[] cr = { new PointF(-26, -100), new PointF(26, -100), new PointF(14, -152), new PointF(-14, -152) };
            g.FillPolygon(SB(RGB(215, 170, 90)), cr);
            g.FillRectangle(SB(RGB(200, 40, 50)), -24, -118, 48, 8);
            using (Pen op = new Pen(RGB(150, 100, 40), 2)) g.DrawEllipse(op, -76, -110, 152, 26);
            return true;
        }
        if (nm == "Basker")
        {
            g.FillEllipse(SB(RGB(180, 30, 40)), -50, -124, 96, 34);
            g.FillRectangle(SB(RGB(130, 20, 30)), -3, -132, 6, 10);
            return true;
        }
        if (nm == "Keps")
        {
            g.FillPie(SB(RGB(40, 120, 220)), -42, -128, 84, 56, 180, 180);
            g.FillEllipse(SB(RGB(25, 80, 160)), 4, -104, 58, 12);
            g.FillEllipse(SB(RGB(25, 80, 160)), -5, -132, 10, 8);
            return true;
        }
        if (nm == "Glass")
        {
            PointF[] cone = { new PointF(-26, -118), new PointF(26, -118), new PointF(0, -90) };
            g.FillPolygon(SB(RGB(225, 180, 100)), cone);
            using (Pen wp = new Pen(RGB(170, 120, 50), 1)) { g.DrawLine(wp, -14, -118, 4, -96); g.DrawLine(wp, 4, -118, -10, -98); g.DrawLine(wp, 16, -118, 6, -100); }
            g.FillEllipse(SB(RGB(255, 150, 190)), -32, -150, 64, 40);
            g.FillEllipse(SB(RGB(255, 245, 235)), -22, -176, 44, 36);
            g.FillEllipse(Brushes.Red, -6, -190, 12, 12);
            return true;
        }
        if (nm == "Svamp")
        {
            g.FillPie(SB(RGB(210, 30, 40)), -52, -150, 104, 100, 180, 180);
            g.FillEllipse(Brushes.White, -32, -128, 18, 13);
            g.FillEllipse(Brushes.White, 8, -138, 20, 15);
            g.FillEllipse(Brushes.White, 26, -116, 13, 10);
            g.FillEllipse(Brushes.White, -10, -112, 11, 8);
            return true;
        }
        if (nm == "Kattöron")
        {
            PointF[] l = { new PointF(-54, -84), new PointF(-36, -134), new PointF(-14, -98) };
            PointF[] r = { new PointF(54, -84), new PointF(36, -134), new PointF(14, -98) };
            g.FillPolygon(SB(RGB(120, 120, 130)), l);
            g.FillPolygon(SB(RGB(120, 120, 130)), r);
            PointF[] li = { new PointF(-46, -90), new PointF(-36, -120), new PointF(-24, -100) };
            PointF[] ri = { new PointF(46, -90), new PointF(36, -120), new PointF(24, -100) };
            g.FillPolygon(SB(RGB(250, 170, 190)), li);
            g.FillPolygon(SB(RGB(250, 170, 190)), ri);
            return true;
        }
        if (nm == "Ballong")
        {
            float bx = (float)Math.Sin(ang * 2) * 12;
            using (Pen sp = new Pen(Color.FromArgb(200, 220, 220, 220), 2)) g.DrawLine(sp, 0, -98, bx, -168);
            g.FillEllipse(SB(RGB(230, 40, 60)), bx - 22, -222, 44, 56);
            g.FillEllipse(SB(Color.FromArgb(150, 255, 255, 255)), bx - 12, -212, 10, 16);
            g.FillPolygon(SB(RGB(190, 20, 40)), new PointF[] { new PointF(bx - 4, -167), new PointF(bx + 4, -167), new PointF(bx, -161) });
            return true;
        }
        if (nm == "Cupcake")
        {
            PointF[] wr = { new PointF(-28, -96), new PointF(28, -96), new PointF(22, -122), new PointF(-22, -122) };
            g.FillPolygon(SB(RGB(240, 140, 180)), wr);
            using (Pen wp = new Pen(RGB(200, 90, 140), 2))
                for (int k = -2; k <= 2; k++) g.DrawLine(wp, k * 10, -96, k * 8, -122);
            g.FillEllipse(SB(RGB(255, 200, 220)), -30, -146, 60, 34);
            g.FillEllipse(SB(RGB(255, 225, 235)), -20, -164, 40, 30);
            g.FillEllipse(Brushes.Red, -5, -174, 11, 11);
            return true;
        }
        if (nm == "Kaninöron")
        {
            float sw = (float)Math.Sin(ang * 2) * 4;
            g.FillEllipse(Brushes.White, -32 - sw, -196, 24, 96);
            g.FillEllipse(Brushes.White, 8 + sw, -196, 24, 96);
            g.FillEllipse(SB(RGB(250, 180, 200)), -26 - sw, -182, 12, 70);
            g.FillEllipse(SB(RGB(250, 180, 200)), 14 + sw, -182, 12, 70);
            return true;
        }
        if (nm == "Lampa")
        {
            float gl = 0.6f + 0.4f * (float)Math.Sin(ang * 5);
            g.FillEllipse(SB(Color.FromArgb((int)(60 * gl), 255, 240, 120)), -50, -190, 100, 110);
            g.FillEllipse(SB(Color.FromArgb((int)(90 * gl), 255, 240, 120)), -32, -170, 64, 74);
            g.FillEllipse(SB(RGB(255, 240, 140)), -17, -158, 34, 42);
            g.FillRectangle(SB(RGB(130, 130, 140)), -10, -118, 20, 14);
            using (Pen fp = new Pen(RGB(230, 150, 30), 2)) g.DrawLine(fp, -5, -128, 5, -144);
            return true;
        }
        if (nm == "Popcorn")
        {
            PointF[] bk = { new PointF(-30, -96), new PointF(30, -96), new PointF(38, -138), new PointF(-38, -138) };
            g.FillPolygon(SB(RGB(220, 40, 50)), bk);
            g.FillRectangle(Brushes.White, -20, -137, 10, 40);
            g.FillRectangle(Brushes.White, 0, -137, 10, 40);
            g.FillRectangle(Brushes.White, 20, -137, 8, 40);
            for (int k = 0; k < 9; k++)
                g.FillEllipse(SB(RGB(255, 245, 205)), -36 + (k % 5) * 17 + (k / 5) * 8, -158 - (k % 3) * 8 - (k / 5) * 8, 17, 17);
            return true;
        }
        if (nm == "Pannkakor")
        {
            for (int k = 0; k < 5; k++)
                g.FillEllipse(SB((k % 2 == 0) ? RGB(225, 170, 80) : RGB(200, 140, 60)), -40, -104 - k * 10, 80, 18);
            g.FillEllipse(SB(RGB(150, 70, 20)), -34, -158, 68, 16);
            g.FillRectangle(SB(RGB(255, 230, 90)), -9, -166, 18, 10);
            return true;
        }
        if (nm == "Fjäderkrona")
        {
            for (int k = 0; k < 7; k++)
            {
                double fa = (-70 + k * 23.3) * Math.PI / 180.0;
                float ex = (float)Math.Sin(fa) * 72, ey = -98 - (float)Math.Cos(fa) * 72;
                using (Pen fp = new Pen(Hsv(k * 50, 0.8, 1.0), 9))
                {
                    fp.StartCap = LineCap.Round; fp.EndCap = LineCap.Round;
                    g.DrawLine(fp, (float)Math.Sin(fa) * 20, -98 - (float)Math.Cos(fa) * 12, ex, ey);
                }
            }
            g.FillRectangle(SB(RGB(190, 40, 40)), -40, -104, 80, 10);
            return true;
        }
        if (nm == "Astronauthjälm")
        {
            g.FillEllipse(SB(Color.FromArgb(55, 170, 225, 255)), -78, -138, 156, 190);
            using (Pen hp = new Pen(Color.FromArgb(220, 235, 240, 250), 6)) g.DrawEllipse(hp, -78, -138, 156, 190);
            g.FillEllipse(SB(Color.FromArgb(150, 255, 255, 255)), -50, -112, 26, 16);
            return true;
        }
        if (nm == "Eldkrona")
        {
            PointF[] cr = { new PointF(-34, -92), new PointF(-34, -120), new PointF(-17, -106), new PointF(0, -126), new PointF(17, -106), new PointF(34, -120), new PointF(34, -92) };
            g.FillPolygon(SB(RGB(255, 190, 40)), cr);
            for (int k = 0; k < 5; k++)
            {
                float h = 18 + (float)Math.Abs(Math.Sin(ang * 8 + k)) * 26;
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(200, 255, 120 + (int)(h * 3), 20)))
                    g.FillEllipse(fb, -34 + k * 17 - 8, -122 - h, 16, h + 8);
            }
            return true;
        }
        if (nm == "Saturnus")
        {
            g.FillEllipse(SB(RGB(230, 180, 110)), -22, -150, 44, 44);
            using (Pen rp = new Pen(RGB(210, 190, 140), 5)) g.DrawEllipse(rp, -52, -136, 104, 16);
            g.FillEllipse(SB(Color.FromArgb(80, 160, 100, 50)), -20, -140, 40, 8);
            return true;
        }
        return false;
    }

    bool DrawEyes2(Graphics g, string nm)
    {
        if (nm == "Katögon")
        {
            g.FillEllipse(SB(RGB(130, 225, 60)), -52, -48, 42, 36);
            g.FillEllipse(SB(RGB(130, 225, 60)), 10, -48, 42, 36);
            g.FillEllipse(Brushes.Black, -34, -48, 7, 36);
            g.FillEllipse(Brushes.Black, 28, -48, 7, 36);
            using (Pen op = new Pen(Color.Black, 2)) { g.DrawEllipse(op, -52, -48, 42, 36); g.DrawEllipse(op, 10, -48, 42, 36); }
            return true;
        }
        if (nm == "Robotögon")
        {
            g.FillRectangle(SB(RGB(30, 32, 40)), -58, -46, 116, 28);
            float sx = (float)Math.Sin(ang * 3) * 42;
            g.FillEllipse(SB(Color.FromArgb(90, 255, 30, 30)), sx - 14, -48, 28, 32);
            g.FillEllipse(SB(RGB(255, 60, 60)), sx - 7, -38, 14, 12);
            return true;
        }
        if (nm == "Kryssögon")
        {
            using (Pen xp = new Pen(Color.Black, 5))
            {
                g.DrawLine(xp, -48, -48, -16, -16); g.DrawLine(xp, -16, -48, -48, -16);
                g.DrawLine(xp, 16, -48, 48, -16); g.DrawLine(xp, 48, -48, 16, -16);
            }
            return true;
        }
        if (nm == "Sömnögon")
        {
            using (Pen ap = new Pen(Color.Black, 4))
            {
                g.DrawArc(ap, -50, -40, 38, 20, 0, 180);
                g.DrawArc(ap, 12, -40, 38, 20, 0, 180);
            }
            float zt = (ang * 40) % 60;
            using (Font zf = new Font("Segoe UI", 16, FontStyle.Bold))
            using (SolidBrush zb = new SolidBrush(Color.FromArgb((int)(255 - zt * 4), 255, 255, 255)))
                g.DrawString("Z", zf, zb, 50 + zt * 0.5f, -70 - zt);
            return true;
        }
        if (nm == "Stjärnögon")
        {
            float sz = 15 + 3 * (float)Math.Sin(ang * 6);
            DrawStar(g, -30, -30, sz, RGB(255, 225, 60));
            DrawStar(g, 30, -30, sz, RGB(255, 225, 60));
            return true;
        }
        if (nm == "Argsint")
        {
            DrawPlainEyes(g, true, true);
            using (Pen bp = new Pen(Color.Black, 6)) { g.DrawLine(bp, -56, -62, -10, -46); g.DrawLine(bp, 56, -62, 10, -46); }
            return true;
        }
        if (nm == "Tårar")
        {
            DrawPlainEyes(g, true, true);
            for (int sd = 0; sd < 2; sd++)
            {
                float ex = (sd == 0) ? -36 : 36;
                float ty = -10 + (ang * 50 + sd * 25) % 70;
                g.FillEllipse(SB(Color.FromArgb(200, 90, 170, 255)), ex - 4, ty, 8, 13);
                using (Pen tp = new Pen(Color.FromArgb(90, 90, 170, 255), 3)) g.DrawLine(tp, ex, -10, ex, ty);
            }
            return true;
        }
        if (nm == "Cyklop")
        {
            g.FillEllipse(Brushes.White, -32, -62, 64, 58);
            g.FillEllipse(SB(RGB(70, 170, 70)), -16, -46, 32, 32);
            g.FillEllipse(Brushes.Black, -8, -38, 16, 16);
            using (Pen op = new Pen(Color.Black, 3)) g.DrawEllipse(op, -32, -62, 64, 58);
            return true;
        }
        if (nm == "Dollarögon")
        {
            StringFormat ds = new StringFormat();
            ds.Alignment = StringAlignment.Center;
            ds.LineAlignment = StringAlignment.Center;
            g.FillEllipse(SB(RGB(60, 200, 90)), -52, -50, 44, 44);
            g.FillEllipse(SB(RGB(60, 200, 90)), 8, -50, 44, 44);
            using (Font df = new Font("Segoe UI", 20, FontStyle.Bold))
            {
                g.DrawString("$", df, Brushes.White, new RectangleF(-52, -50, 44, 44), ds);
                g.DrawString("$", df, Brushes.White, new RectangleF(8, -50, 44, 44), ds);
            }
            return true;
        }
        if (nm == "Tredje ögat")
        {
            DrawPlainEyes(g, true, true);
            float pu = 0.5f + 0.5f * (float)Math.Sin(ang * 4);
            g.FillEllipse(SB(Color.FromArgb((int)(40 + 70 * pu), 190, 90, 255)), -24, -100, 48, 34);
            g.FillEllipse(Brushes.White, -14, -92, 28, 18);
            g.FillEllipse(SB(RGB(90, 60, 220)), -6, -91, 12, 16);
            using (Pen op = new Pen(Color.Black, 2)) g.DrawEllipse(op, -14, -92, 28, 18);
            return true;
        }
        return false;
    }

    bool DrawMouth2(Graphics g, string nm)
    {
        if (nm == "Leende")
        {
            using (Pen mp = new Pen(Color.Black, 4)) g.DrawArc(mp, -30, 2, 60, 40, 20, 140);
            return true;
        }
        if (nm == "Överraskad")
        {
            g.FillEllipse(SB(RGB(120, 15, 30)), -13, 14, 26, 32);
            using (Pen mp = new Pen(Color.Black, 3)) g.DrawEllipse(mp, -13, 14, 26, 32);
            return true;
        }
        if (nm == "Tandgrill")
        {
            g.FillRectangle(Brushes.White, -28, 22, 56, 16);
            using (Pen tp = new Pen(RGB(150, 150, 160), 2))
            {
                for (int k = -3; k <= 3; k++) g.DrawLine(tp, k * 8, 22, k * 8, 38);
                g.DrawRectangle(tp, -28, 22, 56, 16);
            }
            using (Pen bp = new Pen(RGB(200, 205, 215), 3)) g.DrawLine(bp, -28, 30, 28, 30);
            return true;
        }
        if (nm == "Pipa")
        {
            using (Pen sp = new Pen(RGB(110, 70, 30), 5)) g.DrawLine(sp, 0, 30, 20, 34);
            g.FillEllipse(SB(RGB(110, 70, 30)), 14, 24, 22, 24);
            for (int k = 0; k < 3; k++)
            {
                float tt = ((ang * 0.7f) + k * 0.33f) % 1f;
                g.FillEllipse(SB(Color.FromArgb((int)((1 - tt) * 150), 210, 210, 210)), 22 + (float)Math.Sin(ang * 3 + k) * 6, 22 - tt * 50, 10 + tt * 14, 10 + tt * 14);
            }
            return true;
        }
        if (nm == "Näbb")
        {
            PointF[] up = { new PointF(-26, 4), new PointF(26, 4), new PointF(0, 38) };
            PointF[] lo = { new PointF(-16, 24), new PointF(16, 24), new PointF(0, 44) };
            g.FillPolygon(SB(RGB(255, 170, 30)), up);
            g.FillPolygon(SB(RGB(230, 130, 20)), lo);
            return true;
        }
        if (nm == "Grisnos")
        {
            g.FillEllipse(SB(RGB(250, 170, 190)), -28, 0, 56, 38);
            g.FillEllipse(SB(RGB(200, 100, 130)), -14, 12, 9, 14);
            g.FillEllipse(SB(RGB(200, 100, 130)), 5, 12, 9, 14);
            return true;
        }
        if (nm == "Snabel")
        {
            float sw = (float)Math.Sin(ang * 3) * 8;
            using (Pen sp = new Pen(RGB(150, 150, 160), 18))
            {
                sp.StartCap = LineCap.Round; sp.EndCap = LineCap.Round;
                g.DrawCurve(sp, new PointF[] { new PointF(0, 8), new PointF(sw * 0.4f, 40), new PointF(sw, 68), new PointF(sw + 18, 82) });
            }
            return true;
        }
        if (nm == "Munskydd")
        {
            PointF[] mk = { new PointF(-44, 0), new PointF(44, 0), new PointF(38, 50), new PointF(-38, 50) };
            g.FillPolygon(SB(RGB(170, 220, 240)), mk);
            using (Pen pp = new Pen(RGB(120, 180, 210), 2)) { g.DrawLine(pp, -40, 14, 40, 14); g.DrawLine(pp, -38, 28, 38, 28); g.DrawLine(pp, -36, 40, 36, 40); }
            using (Pen ep = new Pen(Color.White, 2)) { g.DrawLine(ep, -44, 4, -76, -10); g.DrawLine(ep, 44, 4, 76, -10); }
            return true;
        }
        if (nm == "Långt skägg")
        {
            PointF[] bd = { new PointF(-62, 0), new PointF(62, 0), new PointF(50, 70), new PointF(24, 130), new PointF(0, 152), new PointF(-24, 130), new PointF(-50, 70) };
            g.FillPolygon(SB(RGB(230, 230, 236)), bd);
            using (Pen op = new Pen(RGB(170, 170, 180), 3)) g.DrawPolygon(op, bd);
            return true;
        }
        if (nm == "Hajtänder")
        {
            g.FillPie(SB(RGB(60, 10, 20)), -34, 4, 68, 50, 0, 180);
            for (int k = 0; k < 8; k++)
            {
                float x = -30 + k * 8;
                g.FillPolygon(Brushes.White, new PointF[] { new PointF(x, 28), new PointF(x + 8, 28), new PointF(x + 4, 40) });
            }
            return true;
        }
        return false;
    }

    bool DrawExtraBack2(Graphics g, string nm)
    {
        if (nm == "Månljus")
        {
            PointF[] cone = { new PointF(-34, -260), new PointF(34, -260), new PointF(120, 160), new PointF(-120, 160) };
            g.FillPolygon(SB(Color.FromArgb(38, 200, 220, 255)), cone);
            return true;
        }
        if (nm == "Laserstrålar")
        {
            for (int k = 0; k < 6; k++)
            {
                double la = (ang * 40 + k * 60) * Math.PI / 180.0;
                using (Pen lp = new Pen(Color.FromArgb(150, Hsv(k * 60, 0.9, 1.0)), 3))
                    g.DrawLine(lp, 0, -5, (float)Math.Cos(la) * 220, -5 + (float)Math.Sin(la) * 220);
            }
            return true;
        }
        return false;
    }

    bool DrawExtraFront2(Graphics g, string nm)
    {
        if (nm == "Fjärilar")
        {
            for (int k = 0; k < 5; k++)
            {
                float a = ang * 0.9f + k * 1.26f;
                float x = (float)Math.Cos(a) * 118, y = (float)Math.Sin(a * 1.4f) * 100 - 10;
                float fl = 3 + (float)Math.Abs(Math.Sin(ang * 14 + k)) * 8;
                Color c = Hsv(k * 70, 0.7, 1.0);
                g.FillEllipse(SB(c), x - 9, y - fl, 9, fl * 2);
                g.FillEllipse(SB(c), x, y - fl, 9, fl * 2);
                g.FillEllipse(Brushes.Black, x - 1, y - 5, 2, 10);
            }
            return true;
        }
        if (nm == "Eldklot")
        {
            for (int k = 0; k < 3; k++)
                for (int tr = 6; tr >= 0; tr--)
                {
                    double a = ang * 2.2 + k * 2.09 - tr * 0.12;
                    float x = (float)Math.Cos(a) * 120, y = (float)Math.Sin(a) * 120 - 5;
                    float sz = 20 - tr * 2.2f;
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(220 - tr * 28, 255, 150 - tr * 15, 20)))
                        g.FillEllipse(b, x - sz / 2, y - sz / 2, sz, sz);
                }
            return true;
        }
        if (nm == "Planeter")
        {
            Color[] pc = { RGB(230, 140, 70), RGB(90, 160, 240), RGB(200, 90, 200) };
            for (int k = 0; k < 3; k++)
            {
                double a = ang * (0.8 + k * 0.3) + k * 2.1;
                float x = (float)Math.Cos(a) * (105 + k * 18), y = (float)Math.Sin(a) * (60 + k * 22) - 5;
                float r = 8 + k * 3;
                g.FillEllipse(SB(pc[k]), x - r, y - r, 2 * r, 2 * r);
                if (k == 1)
                    using (Pen rp = new Pen(RGB(230, 220, 170), 2)) g.DrawEllipse(rp, x - r - 5, y - 3, 2 * r + 10, 6);
            }
            return true;
        }
        if (nm == "Regnmoln")
        {
            for (int k = 0; k < 12; k++)
            {
                float x = -60 + (k * 11) % 120;
                float y = -150 + ((ang * 80 + k * 23) % 120);
                using (Pen dp = new Pen(Color.FromArgb(180, 100, 170, 255), 2)) g.DrawLine(dp, x, y, x - 2, y + 9);
            }
            g.FillEllipse(SB(RGB(150, 155, 170)), -50, -190, 56, 36);
            g.FillEllipse(SB(RGB(150, 155, 170)), -16, -204, 60, 44);
            g.FillEllipse(SB(RGB(150, 155, 170)), 14, -190, 50, 34);
            return true;
        }
        if (nm == "Spöken")
        {
            for (int k = 0; k < 3; k++)
            {
                double a = ang * 1.1 + k * 2.09;
                float x = (float)Math.Cos(a) * 118, y = (float)Math.Sin(a * 1.5) * 90 - 10 + (float)Math.Sin(ang * 5 + k) * 4;
                g.FillEllipse(SB(Color.FromArgb(210, 255, 255, 255)), x - 10, y - 12, 20, 18);
                g.FillRectangle(SB(Color.FromArgb(210, 255, 255, 255)), x - 10, y - 4, 20, 10);
                g.FillEllipse(Brushes.Black, x - 6, y - 7, 4, 5);
                g.FillEllipse(Brushes.Black, x + 2, y - 7, 4, 5);
            }
            return true;
        }
        if (nm == "Tornado")
        {
            for (int k = 0; k < 14; k++)
            {
                float tp = k / 14f;
                float x = (float)Math.Sin(ang * 4 + k * 0.9) * (25 + tp * 75);
                float y = 100 - tp * 220;
                float w = 18 + tp * 26;
                g.FillEllipse(SB(Color.FromArgb(120, 170, 175, 185)), x - w / 2, y - 4, w, 8);
            }
            return true;
        }
        return false;
    }

    // ---------- rita: tillbehör ----------
    void DrawMouth(Graphics g)
    {
        if (equipped[3] < 0) return;
        string nm = items[equipped[3]].Name;
        if (DrawMouth2(g, nm)) return;
        if (nm == "Mustasch")
        {
            g.FillEllipse(SB(RGB(60, 35, 15)), -46, 6, 48, 18);
            g.FillEllipse(SB(RGB(60, 35, 15)), -2, 6, 48, 18);
            g.FillEllipse(SB(RGB(60, 35, 15)), -10, 2, 20, 14);
        }
        else if (nm == "Rosiga kinder")
        {
            g.FillEllipse(SB(Color.FromArgb(150, 255, 110, 150)), -64, -4, 32, 22);
            g.FillEllipse(SB(Color.FromArgb(150, 255, 110, 150)), 32, -4, 32, 22);
        }
        else if (nm == "Clownnäsa")
        {
            g.FillEllipse(SB(RGB(230, 20, 30)), -15, -14, 30, 30);
            g.FillEllipse(SB(Color.FromArgb(180, 255, 255, 255)), -7, -9, 9, 7);
        }
        else if (nm == "Läppar")
        {
            g.FillEllipse(SB(RGB(210, 20, 50)), -26, 18, 52, 24);
            using (Pen p = new Pen(RGB(110, 0, 25), 2)) g.DrawLine(p, -26, 30, 26, 30);
            g.FillEllipse(SB(Color.FromArgb(120, 255, 255, 255)), -12, 21, 14, 5);
        }
        else if (nm == "Vampyrtänder")
        {
            g.FillEllipse(SB(RGB(90, 10, 25)), -28, 16, 56, 26);
            PointF[] f1 = { new PointF(-18, 24), new PointF(-6, 24), new PointF(-12, 46) };
            PointF[] f2 = { new PointF(6, 24), new PointF(18, 24), new PointF(12, 46) };
            g.FillPolygon(Brushes.White, f1);
            g.FillPolygon(Brushes.White, f2);
        }
        else if (nm == "Napp")
        {
            g.FillEllipse(SB(RGB(130, 210, 255)), -24, 14, 48, 28);
            using (Pen p = new Pen(RGB(255, 120, 170), 4)) g.DrawEllipse(p, -10, 38, 20, 20);
            g.FillEllipse(SB(RGB(255, 170, 200)), -7, 21, 14, 14);
        }
        else if (nm == "Tunga")
        {
            float tw = (float)Math.Sin(ang * 5) * 3;
            g.FillEllipse(SB(RGB(240, 100, 130)), -8 + tw, 30, 16, 22 + (float)Math.Sin(ang * 5) * 3);
            using (Pen p = new Pen(RGB(90, 20, 30), 3)) g.DrawArc(p, -28, 6, 56, 36, 20, 140);
        }
        else if (nm == "Tuggummibubbla")
        {
            float sz = 8 + 16 * (float)Math.Abs(Math.Sin(ang * 1.5));
            g.FillEllipse(SB(Color.FromArgb(200, 255, 120, 190)), -sz, 26 - sz, 2 * sz, 2 * sz);
            using (Pen p = new Pen(RGB(230, 70, 150), 2)) g.DrawEllipse(p, -sz, 26 - sz, 2 * sz, 2 * sz);
            g.FillEllipse(SB(Color.FromArgb(150, 255, 255, 255)), -sz * 0.5f, 26 - sz * 0.6f, sz * 0.4f, sz * 0.3f);
        }
        else if (nm == "Guldtand")
        {
            using (Pen p = new Pen(Color.Black, 3)) g.DrawArc(p, -28, 8, 56, 34, 20, 140);
            g.FillRectangle(SB(RGB(255, 215, 60)), 8, 31, 9, 10);
            using (Pen p = new Pen(RGB(190, 140, 10), 1)) g.DrawRectangle(p, 8, 31, 9, 10);
            if (Math.Sin(ang * 5) > 0.6) DrawStar(g, 16, 31, 6, Color.White);
        }
        else if (nm == "Skägg")
        {
            g.FillPie(SB(RGB(110, 70, 30)), -60, -36, 120, 126, 0, 180);
            using (Pen p = new Pen(RGB(70, 40, 15), 3)) g.DrawArc(p, -60, -36, 120, 126, 0, 180);
        }
    }

    void DrawEyes(Graphics g)
    {
        if (equipped[2] < 0) return;
        string nm = items[equipped[2]].Name;
        if (DrawEyes2(g, nm)) return;
        if (nm == "Solglasögon")
        {
            g.FillEllipse(Brushes.Black, -54, -42, 48, 32);
            g.FillEllipse(Brushes.Black, 6, -42, 48, 32);
            using (Pen p = new Pen(Color.Black, 5)) g.DrawLine(p, -8, -28, 8, -28);
            g.FillEllipse(SB(Color.FromArgb(140, 255, 255, 255)), -46, -37, 14, 6);
            g.FillEllipse(SB(Color.FromArgb(140, 255, 255, 255)), 14, -37, 14, 6);
        }
        else if (nm == "Googly-ögon")
        {
            using (Pen p = new Pen(Color.Black, 2))
            {
                g.FillEllipse(Brushes.White, -50, -48, 40, 40); g.DrawEllipse(p, -50, -48, 40, 40);
                g.FillEllipse(Brushes.White, 10, -48, 40, 40); g.DrawEllipse(p, 10, -48, 40, 40);
            }
            float ax = (float)Math.Cos(ang * 3) * 8, ay = (float)Math.Sin(ang * 4) * 8;
            float bx = (float)Math.Cos(ang * 3 + 1.5f) * 8, by = (float)Math.Sin(ang * 4 + 2f) * 8;
            g.FillEllipse(Brushes.Black, -39 + ax, -37 + ay, 18, 18);
            g.FillEllipse(Brushes.Black, 21 + bx, -37 + by, 18, 18);
        }
        else if (nm == "Runda glasögon")
        {
            DrawPlainEyes(g, true, true);
            g.FillEllipse(SB(Color.FromArgb(50, 255, 255, 255)), -52, -50, 44, 44);
            g.FillEllipse(SB(Color.FromArgb(50, 255, 255, 255)), 8, -50, 44, 44);
            using (Pen p = new Pen(RGB(70, 45, 20), 4))
            {
                g.DrawEllipse(p, -52, -50, 44, 44);
                g.DrawEllipse(p, 8, -50, 44, 44);
                g.DrawLine(p, -8, -30, 8, -30);
            }
        }
        else if (nm == "Piratlapp")
        {
            DrawPlainEyes(g, true, false);
            using (Pen p = new Pen(Color.Black, 4))
            {
                g.DrawLine(p, -76, -64, 10, -40);
                g.DrawLine(p, 50, -30, 78, -40);
            }
            g.FillEllipse(Brushes.Black, 8, -50, 44, 44);
        }
        else if (nm == "Monokel")
        {
            DrawPlainEyes(g, true, true);
            using (Pen p = new Pen(RGB(230, 180, 30), 4)) g.DrawEllipse(p, 8, -52, 46, 46);
            using (Pen p = new Pen(RGB(200, 160, 30), 2)) g.DrawLine(p, 31, -6, 46, 64);
        }
        else if (nm == "3D-glasögon")
        {
            DrawPlainEyes(g, true, true);
            g.FillRectangle(SB(Color.FromArgb(170, 235, 40, 40)), -56, -46, 48, 34);
            g.FillRectangle(SB(Color.FromArgb(170, 40, 200, 235)), 8, -46, 48, 34);
            using (Pen p = new Pen(RGB(240, 240, 240), 4))
            {
                g.DrawRectangle(p, -56, -46, 48, 34);
                g.DrawRectangle(p, 8, -46, 48, 34);
                g.DrawLine(p, -8, -32, 8, -32);
            }
        }
        else if (nm == "Spiralögon")
        {
            using (Pen o = new Pen(Color.Black, 2))
            {
                g.FillEllipse(Brushes.White, -52, -50, 44, 44); g.DrawEllipse(o, -52, -50, 44, 44);
                g.FillEllipse(Brushes.White, 8, -50, 44, 44); g.DrawEllipse(o, 8, -50, 44, 44);
            }
            using (Pen p = new Pen(Color.Black, 3))
                for (int side = 0; side < 2; side++)
                {
                    float cx = (side == 0) ? -30 : 30;
                    for (int k = 0; k < 3; k++)
                    {
                        float r = 18 - k * 6;
                        g.DrawArc(p, cx - r, -28 - r, 2 * r, 2 * r, ang * 120f + k * 120f, 280f);
                    }
                }
        }
        else if (nm == "Skidglasögon")
        {
            using (Pen strap = new Pen(RGB(30, 30, 35), 8)) g.DrawLine(strap, -77, -34, 77, -34);
            Rectangle lr = new Rectangle(-60, -54, 120, 44);
            using (LinearGradientBrush lb = new LinearGradientBrush(lr, RGB(255, 190, 60), RGB(210, 80, 20), 45f))
                g.FillEllipse(lb, lr);
            using (Pen fr = new Pen(RGB(30, 30, 35), 5)) g.DrawEllipse(fr, lr);
            g.FillEllipse(SB(Color.FromArgb(120, 255, 255, 255)), -42, -48, 32, 10);
        }
        else if (nm == "Laserögon")
        {
            float pulse = 0.5f + 0.5f * (float)Math.Sin(ang * 8);
            for (int side = 0; side < 2; side++)
            {
                float cx = (side == 0) ? -30 : 30;
                g.FillEllipse(SB(Color.FromArgb(70, 255, 30, 30)), cx - 22, -50, 44, 44);
                g.FillEllipse(SB(Color.FromArgb(130, 255, 40, 40)), cx - 13, -41, 26, 26);
                g.FillEllipse(SB(RGB(255, 230, 230)), cx - 5, -33, 10, 10);
            }
            if (pulse > 0.4f)
                using (Pen p = new Pen(Color.FromArgb((int)(120 + 130 * pulse), 255, 40, 40), 5))
                {
                    g.DrawLine(p, -30, -28, -190, -70);
                    g.DrawLine(p, 30, -28, 190, -70);
                }
        }
        else if (nm == "Hjärtögon")
        {
            float sz = 14 + 3 * (float)Math.Sin(ang * 6);
            DrawHeart(g, -30, -30, sz, RGB(255, 60, 110));
            DrawHeart(g, 30, -30, sz, RGB(255, 60, 110));
        }
    }

    void DrawHead(Graphics g)
    {
        if (equipped[1] < 0) return;
        string nm = items[equipped[1]].Name;
        if (DrawHead2(g, nm)) return;
        if (nm == "Partyhatt")
        {
            PointF[] tri = { new PointF(-38, -92), new PointF(38, -92), new PointF(0, -180) };
            g.FillPolygon(SB(RGB(255, 70, 120)), tri);
            using (Pen p = new Pen(RGB(150, 20, 60), 2)) g.DrawPolygon(p, tri);
            g.FillEllipse(Brushes.Yellow, -8, -135, 9, 9);
            g.FillEllipse(Brushes.Cyan, 10, -112, 9, 9);
            g.FillEllipse(Brushes.White, -22, -108, 9, 9);
            g.FillEllipse(Brushes.Yellow, -9, -190, 18, 18);
        }
        else if (nm == "Hög hatt")
        {
            g.FillRectangle(SB(RGB(30, 30, 34)), -32, -160, 64, 62);
            g.FillEllipse(SB(RGB(30, 30, 34)), -52, -105, 104, 16);
            g.FillRectangle(SB(RGB(190, 30, 40)), -32, -116, 64, 10);
        }
        else if (nm == "Krona")
        {
            PointF[] cr = { new PointF(-42, -92), new PointF(-42, -142), new PointF(-21, -118), new PointF(0, -150),
                            new PointF(21, -118), new PointF(42, -142), new PointF(42, -92) };
            g.FillPolygon(SB(RGB(255, 205, 40)), cr);
            using (Pen p = new Pen(RGB(160, 110, 0), 3)) g.DrawPolygon(p, cr);
            g.FillEllipse(Brushes.Red, -6, -112, 12, 12);
            g.FillEllipse(Brushes.DeepSkyBlue, -32, -108, 9, 9);
            g.FillEllipse(Brushes.LimeGreen, 23, -108, 9, 9);
        }
        else if (nm == "Piratmössa")
        {
            PointF[] pr = { new PointF(-62, -88), new PointF(-26, -136), new PointF(0, -118), new PointF(26, -136), new PointF(62, -88), new PointF(0, -78) };
            g.FillPolygon(Brushes.Black, pr);
            using (Pen p = new Pen(Color.White, 2))
            {
                g.DrawPolygon(p, pr);
                g.DrawLine(p, -14, -100, 14, -88);
                g.DrawLine(p, 14, -100, -14, -88);
            }
            g.FillEllipse(Brushes.White, -9, -118, 18, 15);
            g.FillEllipse(Brushes.Black, -5, -113, 4, 5);
            g.FillEllipse(Brushes.Black, 2, -113, 4, 5);
        }
        else if (nm == "Tomteluva")
        {
            PointF[] tri = { new PointF(-42, -92), new PointF(42, -92), new PointF(8, -170) };
            g.FillPolygon(SB(RGB(210, 20, 30)), tri);
            g.FillEllipse(Brushes.White, -48, -102, 96, 18);
            g.FillEllipse(Brushes.White, -4, -184, 22, 22);
        }
        else if (nm == "Cowboyhatt")
        {
            g.FillEllipse(SB(RGB(150, 95, 40)), -74, -112, 148, 28);
            g.FillRectangle(SB(RGB(170, 110, 50)), -34, -150, 68, 48);
            g.FillEllipse(SB(RGB(170, 110, 50)), -34, -164, 68, 28);
            g.FillRectangle(SB(RGB(90, 50, 20)), -34, -118, 68, 8);
        }
        else if (nm == "Kockhatt")
        {
            g.FillEllipse(Brushes.White, -44, -172, 44, 44);
            g.FillEllipse(Brushes.White, 0, -172, 44, 44);
            g.FillEllipse(Brushes.White, -22, -184, 44, 44);
            g.FillRectangle(Brushes.White, -36, -146, 72, 52);
            using (Pen p = new Pen(RGB(200, 200, 205), 2)) g.DrawRectangle(p, -36, -106, 72, 12);
        }
        else if (nm == "Ängelring")
        {
            float bob = (float)Math.Sin(ang * 2) * 4;
            using (Pen p = new Pen(RGB(255, 215, 60), 6)) g.DrawEllipse(p, -42, -150 + bob, 84, 22);
            using (Pen p = new Pen(Color.FromArgb(120, 255, 255, 200), 12)) g.DrawEllipse(p, -42, -150 + bob, 84, 22);
        }
        else if (nm == "Djävulshorn")
        {
            PointF[] l = { new PointF(-54, -80), new PointF(-36, -134), new PointF(-18, -98) };
            PointF[] r = { new PointF(54, -80), new PointF(36, -134), new PointF(18, -98) };
            g.FillPolygon(SB(RGB(200, 20, 30)), l);
            g.FillPolygon(SB(RGB(200, 20, 30)), r);
            using (Pen p = new Pen(RGB(100, 0, 10), 2)) { g.DrawPolygon(p, l); g.DrawPolygon(p, r); }
        }
        else if (nm == "Trollkarlshatt")
        {
            PointF[] cone = { new PointF(-34, -92), new PointF(34, -92), new PointF(10, -205) };
            g.FillPolygon(SB(RGB(90, 40, 150)), cone);
            g.FillEllipse(SB(RGB(70, 30, 120)), -54, -104, 108, 18);
            g.FillEllipse(Brushes.Yellow, -10, -135, 9, 9);
            g.FillEllipse(Brushes.Yellow, 8, -165, 7, 7);
            g.FillEllipse(Brushes.Yellow, -3, -112, 7, 7);
        }
        else if (nm == "Propellerkeps")
        {
            g.FillPie(SB(RGB(40, 90, 210)), -42, -120, 84, 52, 180, 180);
            g.FillEllipse(SB(RGB(20, 50, 130)), 6, -101, 52, 10);
            g.FillRectangle(Brushes.DimGray, -2, -134, 4, 16);
            GraphicsState st = g.Save();
            g.TranslateTransform(0, -136);
            float sx = Math.Max(0.06f, Math.Abs((float)Math.Cos(ang * 18)));
            g.ScaleTransform(sx, 1f);
            g.FillEllipse(SB(RGB(230, 40, 40)), -30, -4, 60, 8);
            g.Restore(st);
        }
        else if (nm == "Kukirin G2")
        {
            float spin = ang * 25f;
            PointF[] beam = { new PointF(38, -150), new PointF(130, -122), new PointF(130, -178) };
            g.FillPolygon(SB(Color.FromArgb(60, 255, 250, 170)), beam);
            g.FillRectangle(SB(RGB(40, 42, 48)), -42, -118, 84, 9);
            g.FillRectangle(SB(RGB(255, 120, 20)), -42, -111, 84, 2);
            for (int w = 0; w < 2; w++)
            {
                float wx = (w == 0) ? -38 : 38;
                g.FillEllipse(SB(RGB(25, 25, 28)), wx - 13, -121, 26, 26);
                g.FillEllipse(SB(RGB(120, 125, 135)), wx - 6, -114, 12, 12);
                using (Pen sp = new Pen(RGB(255, 140, 30), 2))
                    g.DrawLine(sp, wx + (float)Math.Cos(spin) * 6, -108 + (float)Math.Sin(spin) * 6,
                        wx - (float)Math.Cos(spin) * 6, -108 - (float)Math.Sin(spin) * 6);
            }
            using (Pen fp = new Pen(RGB(255, 120, 20), 3))
            {
                g.DrawArc(fp, -52, -121, 26, 26, 180, 180);
                g.DrawArc(fp, 25, -121, 26, 26, 180, 180);
            }
            using (Pen sp2 = new Pen(RGB(35, 36, 42), 7)) g.DrawLine(sp2, 36, -114, 28, -172);
            using (Pen hp = new Pen(RGB(35, 36, 42), 6)) g.DrawLine(hp, 16, -177, 42, -171);
            g.FillEllipse(SB(RGB(255, 120, 20)), 30, -136, 9, 9);
            g.FillRectangle(SB(RGB(20, 20, 24)), 22, -192, 18, 11);
            g.FillRectangle(SB(RGB(60, 255, 120)), 24, -189, 14, 3);
            g.FillEllipse(SB(RGB(255, 245, 160)), 35, -154, 9, 9);
            if (Math.Sin(ang * 6) > 0) g.FillEllipse(SB(RGB(255, 40, 40)), -58, -116, 8, 6);
            StringFormat ks = new StringFormat();
            ks.Alignment = StringAlignment.Center;
            ks.LineAlignment = StringAlignment.Center;
            using (Font kf = new Font("Segoe UI", 6f, FontStyle.Bold))
                g.DrawString("KUKIRIN G2", kf, Brushes.White, new RectangleF(-26, -119, 52, 11), ks);
        }
        else if (nm == "Pizzaskiva")
        {
            PointF[] sl = { new PointF(-46, -92), new PointF(46, -92), new PointF(0, -168) };
            g.FillPolygon(SB(RGB(250, 200, 70)), sl);
            g.FillEllipse(SB(RGB(200, 130, 50)), -50, -102, 100, 16);
            g.FillEllipse(SB(RGB(210, 40, 40)), -7, -130, 14, 14);
            g.FillEllipse(SB(RGB(210, 40, 40)), -22, -110, 14, 14);
            g.FillEllipse(SB(RGB(210, 40, 40)), 8, -114, 14, 14);
            g.FillEllipse(SB(RGB(60, 160, 60)), -2, -152, 6, 6);
            g.FillEllipse(SB(RGB(60, 160, 60)), -14, -94, 6, 6);
        }
        else if (nm == "Badanka")
        {
            g.FillEllipse(SB(RGB(255, 225, 60)), -36, -120, 72, 36);
            g.FillEllipse(SB(RGB(245, 190, 40)), -22, -110, 32, 16);
            g.FillEllipse(SB(RGB(255, 225, 60)), 4, -152, 36, 36);
            g.FillEllipse(SB(RGB(255, 140, 20)), 34, -138, 22, 11);
            g.FillEllipse(Brushes.Black, 22, -142, 5, 5);
        }
        else if (nm == "Ufo")
        {
            PointF[] ray = { new PointF(-30, -100), new PointF(30, -100), new PointF(62, 70), new PointF(-62, 70) };
            g.FillPolygon(SB(Color.FromArgb(32, 190, 255, 140)), ray);
            g.FillPie(SB(Color.FromArgb(190, 120, 230, 255)), -30, -150, 60, 52, 180, 180);
            g.FillEllipse(SB(RGB(150, 155, 170)), -58, -126, 116, 26);
            for (int k = 0; k < 5; k++)
            {
                bool on = (((int)(ang * 6) + k) % 2 == 0);
                g.FillEllipse(SB(on ? RGB(255, 235, 80) : RGB(210, 50, 50)), -42 + k * 21 - 4, -117, 8, 8);
            }
        }
        else if (nm == "Raket")
        {
            float fl = 14 + (float)Math.Abs(Math.Sin(ang * 25)) * 14;
            PointF[] f1 = { new PointF(-11, -120), new PointF(11, -120), new PointF(0, -120 + fl) };
            PointF[] f2 = { new PointF(-6, -120), new PointF(6, -120), new PointF(0, -120 + fl * 0.6f) };
            g.FillPolygon(SB(RGB(255, 150, 20)), f1);
            g.FillPolygon(SB(RGB(255, 235, 90)), f2);
            PointF[] fl1 = { new PointF(-16, -142), new PointF(-32, -116), new PointF(-16, -126) };
            PointF[] fl2 = { new PointF(16, -142), new PointF(32, -116), new PointF(16, -126) };
            g.FillPolygon(SB(RGB(210, 40, 40)), fl1);
            g.FillPolygon(SB(RGB(210, 40, 40)), fl2);
            g.FillEllipse(Brushes.White, -16, -192, 32, 74);
            g.FillPie(SB(RGB(210, 40, 40)), -16, -194, 32, 38, 180, 180);
            g.FillEllipse(SB(RGB(80, 170, 235)), -7, -166, 14, 14);
            using (Pen p = new Pen(RGB(120, 120, 130), 2)) g.DrawEllipse(p, -7, -166, 14, 14);
        }
        else if (nm == "Diamantkrona")
        {
            PointF[] cr = { new PointF(-48, -90), new PointF(-48, -152), new PointF(-24, -124), new PointF(0, -168),
                            new PointF(24, -124), new PointF(48, -152), new PointF(48, -90) };
            g.FillPolygon(SB(RGB(215, 240, 255)), cr);
            using (Pen p = new Pen(RGB(60, 150, 210), 3)) g.DrawPolygon(p, cr);
            g.FillEllipse(Brushes.DeepSkyBlue, -9, -120, 18, 18);
            g.FillEllipse(Brushes.DodgerBlue, -38, -112, 11, 11);
            g.FillEllipse(Brushes.DodgerBlue, 27, -112, 11, 11);
            if (Math.Sin(ang * 5) > 0) DrawStar(g, 0, -170, 9, Color.White);
            if (Math.Sin(ang * 5 + 2) > 0) DrawStar(g, -48, -154, 7, Color.White);
            if (Math.Sin(ang * 5 + 4) > 0) DrawStar(g, 48, -154, 7, Color.White);
        }
    }

    void DrawExtraBack(Graphics g)
    {
        if (equipped[4] < 0) return;
        string nm = items[equipped[4]].Name;
        if (DrawExtraBack2(g, nm)) return;
        if (nm == "Aura")
        {
            for (int k = 0; k < 6; k++)
            {
                float ex = 80 + k * 10 + (float)Math.Sin(ang * 3) * 4;
                float ey = 105 + k * 10 + (float)Math.Sin(ang * 3) * 4;
                g.FillEllipse(SB(Color.FromArgb(70 - k * 10, 255, 240, 120)), -ex, -5 - ey, 2 * ex, 2 * ey);
            }
        }
        else if (nm == "Disco")
        {
            for (int k = 0; k < 6; k++)
                using (SolidBrush b = new SolidBrush(Color.FromArgb(70, Hsv(k * 60, 0.7, 1.0))))
                    g.FillPie(b, -170, -175, 340, 340, ang * 60f + k * 60f, 30f);
        }
        else if (nm == "Regnbågsring")
        {
            for (int k = 0; k < 7; k++)
            {
                float ex = 88 + k * 6, ey = 113 + k * 6;
                using (Pen p = new Pen(Hsv(k * 50 + ang * 40, 0.8, 1.0), 5))
                    g.DrawEllipse(p, -ex, -5 - ey, 2 * ex, 2 * ey);
            }
        }
    }

    void DrawExtraFront(Graphics g)
    {
        if (equipped[4] < 0) return;
        string nm = items[equipped[4]].Name;
        if (DrawExtraFront2(g, nm)) return;
        if (nm == "Gnistor")
        {
            for (int k = 0; k < 10; k++)
            {
                float a = ang * 1.5f + k * 0.63f;
                float r = 105 + (k % 3) * 12;
                float x = (float)Math.Cos(a) * r * 0.8f, y = (float)Math.Sin(a * 1.3f) * r * 0.9f - 5;
                int al = (int)(135 + 120 * Math.Sin(ang * 6 + k));
                using (SolidBrush b = new SolidBrush(Color.FromArgb(al, 255, 245, 160)))
                    g.FillEllipse(b, x - 3, y - 3, 6, 6);
            }
        }
        else if (nm == "Stjärnor")
        {
            for (int k = 0; k < 6; k++)
            {
                float a = ang * 0.8f + k * 1.047f;
                float x = (float)Math.Cos(a) * 115, y = (float)Math.Sin(a) * 125 - 5;
                DrawStar(g, x, y, 9 + 3 * (float)Math.Sin(ang * 4 + k), Color.FromArgb(255, 255, 220, 60));
            }
        }
        else if (nm == "Hjärtan")
        {
            for (int k = 0; k < 5; k++)
            {
                float tt = (ang * 0.5f + k * 0.2f) % 1f;
                float x = (k - 2) * 36 + (float)Math.Sin(ang * 2 + k) * 10;
                float y = 100 - tt * 230;
                DrawHeart(g, x, y, 9, Color.FromArgb((int)((1 - tt) * 255), 255, 90, 140));
            }
        }
        else if (nm == "Snöfall")
        {
            for (int k = 0; k < 16; k++)
            {
                float x = ((k * 37) % 200) - 100 + (float)Math.Sin(ang * 2 + k) * 8;
                float y = -130 + ((ang * 30 + k * 41) % 260);
                g.FillEllipse(SB(Color.FromArgb(220, 255, 255, 255)), x - 3, y - 3, 6, 6);
            }
        }
        else if (nm == "Eld")
        {
            for (int k = 0; k < 14; k++)
            {
                float tt = (ang * 1.2f + k * 0.0714f) % 1f;
                float x = (float)Math.Sin(k * 2.4) * 70 * (1 - tt * 0.3f);
                float y = 95 - tt * 160;
                float sz = 20 * (1 - tt) + 4;
                using (SolidBrush b = new SolidBrush(Color.FromArgb((int)((1 - tt) * 230), 255, (int)(230 - 190 * tt), 0)))
                    g.FillEllipse(b, x - sz / 2, y - sz / 2, sz, sz);
            }
        }
        else if (nm == "Stinkmoln")
        {
            for (int k = 0; k < 3; k++)
            {
                PointF[] pts = new PointF[12];
                for (int j = 0; j < 12; j++)
                    pts[j] = new PointF(-40 + k * 40 + (float)Math.Sin(j * 0.7 + ang * 3 + k) * 8, -120 - j * 9);
                using (Pen p = new Pen(Color.FromArgb(150, 140, 200, 60), 4)) g.DrawCurve(p, pts);
            }
        }
        else if (nm == "Konfetti")
        {
            for (int k = 0; k < 24; k++)
            {
                float x = ((k * 53) % 240) - 120;
                float y = -140 + ((ang * 45 + k * 37) % 300);
                using (SolidBrush b = new SolidBrush(Hsv(k * 15, 0.9, 1.0))) g.FillRectangle(b, x, y, 6, 4);
            }
        }
        else if (nm == "Musik")
        {
            for (int k = 0; k < 4; k++)
            {
                float tt = (ang * 0.4f + k * 0.25f) % 1f;
                float x = -90 + k * 55 + (float)Math.Sin(ang * 3 + k) * 8;
                float y = 60 - tt * 190;
                using (Font f = new Font("Segoe UI Symbol", 22, FontStyle.Bold))
                using (SolidBrush b = new SolidBrush(Color.FromArgb((int)((1 - tt) * 255), 255, 255, 255)))
                    g.DrawString((k % 2 == 0) ? "\u266A" : "\u266B", f, b, x, y);
            }
        }
        else if (nm == "Bubblor")
        {
            for (int k = 0; k < 8; k++)
            {
                float tt = (ang * 0.5f + k * 0.125f) % 1f;
                float x = (float)Math.Sin(k * 1.9) * 95 + (float)Math.Sin(ang * 2 + k) * 6;
                float y = 105 - tt * 240;
                float r = 7 + (k % 3) * 4;
                int al = (int)(200 * (1 - tt));
                using (SolidBrush b = new SolidBrush(Color.FromArgb(al / 4, 150, 230, 255)))
                    g.FillEllipse(b, x - r, y - r, 2 * r, 2 * r);
                using (Pen p = new Pen(Color.FromArgb(al, 255, 255, 255), 2))
                    g.DrawEllipse(p, x - r, y - r, 2 * r, 2 * r);
            }
        }
        else if (nm == "Åska")
        {
            if (Math.Sin(ang * 3.7) > 0.9)
            {
                PointF[] l = { new PointF(-100, -100), new PointF(-84, -65), new PointF(-96, -62), new PointF(-78, -20) };
                PointF[] r = { new PointF(100, -90), new PointF(84, -55), new PointF(96, -52), new PointF(78, -10) };
                using (Pen p = new Pen(RGB(255, 255, 80), 4)) { g.DrawLines(p, l); g.DrawLines(p, r); }
            }
        }
    }

    void DrawAccessories(Graphics g)
    {
        DrawBodyFront(g);
        DrawMouth(g);
        DrawEyes(g);
        DrawHead(g);
        DrawExtraFront(g);
    }

    // ---------- rita: allt ----------
    protected override void OnPaint(PaintEventArgs e)
    {
        try { Render(e.Graphics); }
        catch (Exception ex) { LogError(ex); }
    }

    void Render(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        DrawBackground(g);
        g.TranslateTransform(ox + shX, oy + shY);
        g.ScaleTransform(sc, sc);

        StringFormat sf = new StringFormat();
        sf.Alignment = StringAlignment.Center;
        Color yel = Color.FromArgb(233, 242, 122);

        foreach (Cloud c in clouds)
            using (SolidBrush b = new SolidBrush(Color.FromArgb(c.A, 154, 205, 50)))
                g.FillEllipse(b, c.X - c.S / 2, c.Y - c.S / 2, c.S, c.S);

        using (Font f = new Font("Segoe UI", 34, FontStyle.Bold))
        using (SolidBrush b = new SolidBrush(yel))
            g.DrawString("RUTTET ÄGG", f, b, new RectangleF(0, 25, 420, 70), sf);
        using (Font f = new Font("Segoe UI", 12, FontStyle.Bold))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 214, 64)))
            g.DrawString((playerName.Length > 0 ? playerName + "   •   " : "") + "Nivå " + lvl + "   •   x" + Mult().ToString("0.0") + " multiplier", f, b, new RectangleF(0, 80, 420, 24), sf);
        if (equipped[10] >= 0)
            using (Font tf2 = new Font("Segoe UI", 11, FontStyle.Bold))
            using (SolidBrush tb2 = new SolidBrush(items[equipped[10]].L))
                g.DrawString("\u00AB " + items[equipped[10]].Name + " \u00BB", tf2, tb2, new RectangleF(0, 101, 420, 20), sf);

        // ägget
        if (popTimer == 0)
        {
            Color lc, dc;
            EggColors(out lc, out dc);
            float es = Math.Max(0.03f, eggScale) * (1f + level / 600f);
            GraphicsState st = g.Save();
            g.TranslateTransform(210, 330);
            g.RotateTransform((float)Math.Sin(ang * (1 + level / 40f)) * (8f + level / 8f));
            g.ScaleTransform(es, es);
            DrawBodyBack(g);
            DrawExtraBack(g);
            Rectangle er = new Rectangle(-80, -110, 160, 210);
            using (LinearGradientBrush lb = new LinearGradientBrush(er, lc, dc, 60f))
                g.FillEllipse(lb, er);
            DrawPattern(g, items[equipped[0]].Pat, items[equipped[0]].P);
            if (items[equipped[0]].Name == "Galax")
            {
                float[] gx = { -40, 30, -20, 45, -50, 10, 0, 55, -60 };
                float[] gy = { -60, -40, 10, 40, 50, 70, -85, -10, -5 };
                for (int k = 0; k < gx.Length; k++)
                {
                    int al = (int)(150 + 100 * Math.Sin(ang * 4 + k * 1.7));
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(al, 255, 255, 255)))
                        g.FillEllipse(b, gx[k] - 2.5f, gy[k] - 2.5f, 5, 5);
                }
            }
            else
            {
                using (SolidBrush sp = new SolidBrush(Color.FromArgb(90, dc)))
                {
                    g.FillEllipse(sp, -40, -50, 26, 20);
                    g.FillEllipse(sp, 15, 10, 34, 26);
                    g.FillEllipse(sp, -30, 40, 18, 14);
                }
            }
            using (Pen p = new Pen(Color.FromArgb(200, dc), 3)) g.DrawEllipse(p, er);
            if (level > 30)
                using (Pen cp = new Pen(Color.FromArgb(200, 40, 30, 10), 2.5f))
                {
                    g.DrawLines(cp, new PointF[] { new PointF(-10, -108), new PointF(-22, -80), new PointF(-8, -62), new PointF(-30, -35) });
                    if (level > 55)
                        g.DrawLines(cp, new PointF[] { new PointF(40, -95), new PointF(28, -70), new PointF(46, -48), new PointF(30, -25), new PointF(52, 0) });
                    if (level > 80)
                        g.DrawLines(cp, new PointF[] { new PointF(-60, 20), new PointF(-35, 32), new PointF(-45, 55), new PointF(-15, 70) });
                }
            DrawAccessories(g);
            g.Restore(st);
        }

        DrawPet(g);

        // flugor
        for (int i = 0; i < 6; i++)
        {
            float a = ang * (1 + i * 0.2f) + i;
            float x = 210 + (float)Math.Cos(a * 1.3f) * (110 + i * 12);
            float y = 300 + (float)Math.Sin(a * 2f) * (90 + i * 8);
            using (SolidBrush w = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
                g.FillEllipse(w, x - 6, y - 8, 8, 8);
            g.FillEllipse(Brushes.Black, x - 4, y - 3, 9, 7);
        }

        // partiklar från poppen
        foreach (Part p in parts)
        {
            int al = Math.Max(0, Math.Min(255, p.L * 255 / p.M));
            Color pcol = Color.FromArgb(al, p.C);
            if (p.Sh == 1)
            {
                using (SolidBrush b = new SolidBrush(pcol)) g.FillRectangle(b, p.X - p.S / 2, p.Y - p.S / 3, p.S, p.S * 0.6f);
            }
            else if (p.Sh == 2) DrawStar(g, p.X, p.Y, p.S, pcol);
            else if (p.Sh == 3) DrawHeart(g, p.X, p.Y, p.S * 0.6f, pcol);
            else if (p.Sh == 4)
            {
                using (Pen pp = new Pen(pcol, 2)) g.DrawEllipse(pp, p.X - p.S / 2, p.Y - p.S / 2, p.S, p.S);
            }
            else
            {
                using (SolidBrush b = new SolidBrush(pcol)) g.FillEllipse(b, p.X - p.S / 2, p.Y - p.S / 2, p.S, p.S);
            }
        }
        foreach (Floater fl in floats)
        {
            int al = Math.Min(255, fl.L * 6);
            using (Font f = new Font("Segoe UI", 28, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(al, 255, 214, 64)))
                g.DrawString(fl.T, f, b, new RectangleF(fl.X - 100, fl.Y, 200, 50), sf);
            if (fl.Coin) DrawCoin(g, fl.X + 50, fl.Y + 24, 12);
        }

        using (Font f = new Font("Segoe UI", 12))
        using (SolidBrush b = new SolidBrush(yel))
        {
            g.DrawString(msg, f, b, new RectangleF(20, 440, 380, 50), sf);
            g.DrawString("Stanknivå: " + (int)Math.Min(level, 100) + "%" + (level > 80 ? "  !!!" : ""), f, b, new RectangleF(0, 520, 420, 24), sf);
        }

        // mätare
        Rectangle mr = new Rectangle(60, 492, 300, 18);
        using (Pen p = new Pen(yel, 2)) g.DrawRectangle(p, mr);
        int w2 = (int)(296 * Math.Min(level, 100) / 100f);
        Color mc = level < 50 ? Color.FromArgb(154, 205, 50) : (level < 80 ? Color.FromArgb(201, 164, 0) : Color.FromArgb(122, 59, 0));
        using (SolidBrush b = new SolidBrush(mc)) g.FillRectangle(b, 62, 494, w2, 15);

        // knappen
        StringFormat cf = new StringFormat();
        cf.Alignment = StringAlignment.Center;
        cf.LineAlignment = StringAlignment.Center;
        using (SolidBrush b = new SolidBrush(Color.FromArgb(182, 214, 58)))
            g.FillRectangle(b, btn);
        using (Font f = new Font("Segoe UI", 11, FontStyle.Bold))
            g.DrawString(btnText, f, Brushes.Black, btn, cf);

        // ---------- gränssnitt i skärmkoordinater ----------
        g.ResetTransform();
        float u = UI();
        int W = ClientSize.Width;

        // poängräknare uppe i högra hörnet
        float cr2 = 18 * u * (1f + 0.35f * coinPulse);
        float ccx = W - 38 * u, ccy = 38 * u;
        StringFormat nf = new StringFormat();
        nf.Alignment = StringAlignment.Far;
        nf.LineAlignment = StringAlignment.Center;
        using (Font f = new Font("Segoe UI", 26 * u, FontStyle.Bold))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 214, 64)))
            g.DrawString(coins.ToString(), f, b, new RectangleF(W - 66 * u - 300 * u, 18 * u, 300 * u, 40 * u), nf);
        DrawCoin(g, ccx, ccy, cr2);
        using (Font f = new Font("Segoe UI", 10f * u))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(200, 233, 242, 122)))
            g.DrawString((savedFlash > 0 ? "Sparat \u2713     " : "") + "Poppade ägg: " + pops, f, b,
                new RectangleF(W - 340 * u, 56 * u, 320 * u, 20 * u), nf);

        // butiken
        BuildShop();
        using (SolidBrush b = new SolidBrush(Color.FromArgb(150, 15, 20, 5))) g.FillRectangle(b, shopPanel);
        using (Pen p = new Pen(Color.FromArgb(120, 233, 242, 122), 2)) g.DrawRectangle(p, shopPanel.X, shopPanel.Y, shopPanel.Width, shopPanel.Height);
        using (Font f = new Font("Segoe UI", 17 * u, FontStyle.Bold))
        using (SolidBrush b = new SolidBrush(yel))
            g.DrawString("BUTIK", f, b, new RectangleF(shopPanel.X, shopPanel.Y + 8 * u, shopPanel.Width, 32 * u), sf);

        // rebirth-knappen
        {
            int cost = RebirthCost();
            bool afford = coins >= cost;
            bool armed = rebirthArm > 0;
            Color rf2 = armed ? Color.FromArgb(255, 140, 0) : (afford ? Color.FromArgb(230, 190, 40) : Color.FromArgb(180, 55, 60, 35));
            if (hoverRebirth && afford && !armed) rf2 = Color.FromArgb(255, 215, 70);
            using (SolidBrush b = new SolidBrush(rf2)) g.FillRectangle(b, shopRebirth);
            using (Pen p = new Pen(Color.FromArgb(200, 255, 235, 120), 2)) g.DrawRectangle(p, shopRebirth.X, shopRebirth.Y, shopRebirth.Width, shopRebirth.Height);
            Brush tb = afford ? Brushes.Black : Brushes.LightGray;
            RectangleF l1 = new RectangleF(shopRebirth.X, shopRebirth.Y + 2 * u, shopRebirth.Width, shopRebirth.Height / 2f);
            RectangleF l2 = new RectangleF(shopRebirth.X, shopRebirth.Y + shopRebirth.Height / 2f - 2 * u, shopRebirth.Width, shopRebirth.Height / 2f);
            using (Font f = new Font("Segoe UI", 11f * u, FontStyle.Bold))
                g.DrawString(armed ? "KLICKA IGEN FÖR ATT BEKRÄFTA!" : "REBIRTH  \u2192  NIVÅ " + (lvl + 1), f, tb, l1, cf);
            using (Font f = new Font("Segoe UI", 9f * u))
                g.DrawString("Kostar " + cost + " coins   •   x" + (1f + 0.1f * (lvl + 1)).ToString("0.0") + " multiplier", f, tb, l2, cf);
        }

        // flikar
        for (int i = 0; i < tabNames.Length; i++)
        {
            bool act = (tab == i);
            Color tf = act ? Color.FromArgb(182, 214, 58) : (hoverTab == i ? Color.FromArgb(200, 60, 75, 16) : Color.FromArgb(170, 25, 32, 8));
            using (SolidBrush b = new SolidBrush(tf)) g.FillRectangle(b, shopTabs[i]);
            using (Font f = new Font("Segoe UI", 7.5f * u, FontStyle.Bold))
                g.DrawString(tabNames[i], f, act ? Brushes.Black : SB(yel), shopTabs[i], cf);
            if (TabHasNew(i))
                g.FillEllipse(SB(Color.FromArgb(255, 80, 60)), shopTabs[i].Right - 10 * u, shopTabs[i].Y + 2 * u, 8 * u, 8 * u);
        }

        StringFormat lf = new StringFormat();
        lf.Alignment = StringAlignment.Near;
        lf.LineAlignment = StringAlignment.Center;
        lf.FormatFlags = StringFormatFlags.NoWrap;
        StringFormat rf = new StringFormat();
        rf.Alignment = StringAlignment.Far;
        rf.LineAlignment = StringAlignment.Center;

        if (shopMax > 0)
        {
            float trx = shopList.Right - 7 * u, tr0 = shopList.Y, trh = shopList.Height;
            g.FillRectangle(SB(Color.FromArgb(90, 255, 255, 255)), trx, tr0, 5 * u, trh);
            float th = Math.Max(24 * u, trh * VISROWS / (float)shopCount);
            float ty = tr0 + (trh - th) * scroll[tab] / (float)shopMax;
            g.FillRectangle(SB(Color.FromArgb(220, 233, 242, 122)), trx, ty, 5 * u, th);
            using (Font hf = new Font("Segoe UI", 8f * u))
                g.DrawString("Scrolla med mushjulet (" + shopCount + " saker)", hf, SB(Color.FromArgb(160, 233, 242, 122)),
                    new RectangleF(shopList.X, shopList.Bottom + 1 * u, shopList.Width, 14 * u), sf);
        }
        foreach (ShopRow r in shopRows)
        {
            int i = r.Item;
            Item it = items[i];
            bool own = owned[i];
            bool locked = it.Lvl > lvl;
            bool eq = equipped[it.Slot] == i;
            bool afford = coins >= it.Price;
            Color fill = locked ? Color.FromArgb(150, 20, 22, 10) : (eq ? Color.FromArgb(210, 90, 110, 20) : (hover == i ? Color.FromArgb(200, 60, 75, 16) : Color.FromArgb(170, 25, 32, 8)));
            using (SolidBrush b = new SolidBrush(fill)) g.FillRectangle(b, r.R);
            using (Pen p = new Pen(eq ? Color.FromArgb(255, 233, 242, 122) : Color.FromArgb(90, 233, 242, 122), eq ? 2f : 1f))
                g.DrawRectangle(p, r.R.X, r.R.Y, r.R.Width, r.R.Height);
            if (it.Slot == 5 && own && !locked)
            {
                float fr = Math.Max(0f, Math.Min(1f, (float)dur[i] / it.MaxDur));
                g.FillRectangle(SB(Color.FromArgb(120, 0, 0, 0)), r.R.X + 2, r.R.Bottom - 5 * u, r.R.Width - 4, 3 * u);
                g.FillRectangle(SB(Lerp(RGB(230, 60, 40), RGB(110, 220, 70), fr)), r.R.X + 2, r.R.Bottom - 5 * u, (r.R.Width - 4) * fr, 3 * u);
            }

            float tx = r.R.X + 10 * u;
            if (it.Slot == 0)
            {
                Color l2, d2;
                ItemColors(it, out l2, out d2);
                RectangleF sw = new RectangleF(r.R.X + 8 * u, r.R.Y + 9 * u, 20 * u, 20 * u);
                using (LinearGradientBrush lb = new LinearGradientBrush(sw, l2, d2, 60f))
                    g.FillEllipse(lb, sw);
                tx = r.R.X + 36 * u;
            }
            else if (it.Slot == 5)
            {
                DrawTool(g, it, r.R.X + 22 * u, r.R.Y + 19 * u, 0.2f * u, 40f);
                tx = r.R.X + 46 * u;
            }
            else if (it.Slot == 8 || it.Slot == 9)
            {
                RectangleF bsw = new RectangleF(r.R.X + 8 * u, r.R.Y + 9 * u, 28 * u, 20 * u);
                using (LinearGradientBrush bl = new LinearGradientBrush(bsw, it.L, it.D, 90f))
                    g.FillRectangle(bl, bsw);
                tx = r.R.X + 44 * u;
            }
            using (Font f = new Font("Segoe UI", 11f * u))
                g.DrawString(it.Name, f, locked ? SB(Color.FromArgb(150, 150, 150, 150)) : ((it.Slot == 10) ? SB(it.L) : Brushes.White),
                    new RectangleF(tx, r.R.Y, r.R.Width - (tx - r.R.X) - 70 * u, (it.Slot == 5) ? r.R.Height * 0.6f : r.R.Height), lf);
            if (it.Slot == 5)
                using (Font f2 = new Font("Segoe UI", 7.5f * u))
                    g.DrawString("+" + it.Power + " hugg  \u2022  " + it.MaxDur + " håll  \u2022  +" + it.Bonus + "% mynt", f2,
                        SB(Color.FromArgb(180, 200, 200, 170)),
                        new RectangleF(tx, r.R.Y + r.R.Height * 0.55f, r.R.Width - (tx - r.R.X) - 84 * u, r.R.Height * 0.45f), lf);

            using (Font f = new Font("Segoe UI", 11f * u, FontStyle.Bold))
            {
                if (locked)
                {
                    float lx = r.R.Right - 18 * u, ly = r.R.Y + r.R.Height / 2f;
                    g.FillRectangle(SB(Color.FromArgb(200, 170, 170, 170)), lx - 6 * u, ly - 1 * u, 12 * u, 9 * u);
                    using (Pen p = new Pen(Color.FromArgb(200, 170, 170, 170), 2f * u))
                        g.DrawArc(p, lx - 4 * u, ly - 8 * u, 8 * u, 10 * u, 180, 180);
                    g.DrawString("Nivå " + it.Lvl, f, SB(Color.FromArgb(200, 170, 170, 170)),
                        new RectangleF(r.R.Right - 100 * u, r.R.Y, 76 * u, r.R.Height), rf);
                }
                else if (own)
                {
                    g.DrawString((it.Slot == 5) ? (dur[i] + "/" + it.MaxDur) : (eq ? "PÅ" : "ÄGD"), f, eq ? SB(yel) : SB(Color.FromArgb(190, 200, 200, 200)),
                        new RectangleF(r.R.Right - 80 * u, r.R.Y, 70 * u, r.R.Height), rf);
                }
                else
                {
                    g.DrawString(it.Price.ToString(), f, afford ? SB(Color.FromArgb(255, 214, 64)) : SB(Color.FromArgb(200, 140, 120)),
                        new RectangleF(r.R.Right - 80 * u, r.R.Y, 52 * u, r.R.Height), rf);
                    DrawCoin(g, r.R.Right - 16 * u, r.R.Y + r.R.Height / 2f, 8 * u);
                }
            }
        }

        if (equipped[5] >= 0 && !shopPanel.Contains(mx, my))
        {
            Item tl = items[equipped[5]];
            float low = (float)dur[equipped[5]] / tl.MaxDur;
            float wob = (low < 0.2f) ? (float)Math.Sin(tickN * 2.0) * 3f : 0f;
            DrawTool(g, tl, mx + wob, my, 1.25f * u, -25f + 60f * swing);
        }
        using (Font f = new Font("Segoe UI", 9))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(130, yel)))
            g.DrawString("Klicka på ägget eller tryck mellanslag för att poppa det snabbare.   (Esc = avsluta)   v" + MyVersion(), f, b,
                new RectangleF(0, ClientSize.Height - 28, ClientSize.Width, 20), sf);
        string us = updStatus;
        if (updText.Length == 0 && us.Length > 0)
        {
            bool bad = us.IndexOf("misslyck") >= 0 || us.IndexOf("gick inte") >= 0 || us.IndexOf("fel") >= 0
                || us.IndexOf("Ingen uppdateringsadress") >= 0 || us.IndexOf("Hittar inte") >= 0 || us.IndexOf("innehåller inte") >= 0;
            if (bad || us.StartsWith("Hämtar") || us.StartsWith("Bygger") || tickN < 300)
                using (Font f = new Font("Segoe UI", 9))
                using (SolidBrush b = new SolidBrush(bad ? Color.FromArgb(255, 255, 130, 110) : Color.FromArgb(170, 233, 242, 122)))
                    g.DrawString(us, f, b, new RectangleF(12, ClientSize.Height - 50, ClientSize.Width * 0.7f, 20));
        }
        if (updText.Length > 0)
        {
            // stor ruta mitt på skärmen när en ny version är hämtad
            float pulse = 0.5f + 0.5f * (float)Math.Sin(ang * 4);
            float bw = Math.Min(ClientSize.Width - 40f, 900f * u), bh = 250f * u;
            RectangleF br = new RectangleF((ClientSize.Width - bw) / 2f, (ClientSize.Height - bh) / 2f, bw, bh);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(232, 20, 25, 6))) g.FillRectangle(b, br);
            using (Pen p = new Pen(Color.FromArgb((int)(150 + 105 * pulse), 255, 214, 64), 6f * u))
                g.DrawRectangle(p, br.X, br.Y, br.Width, br.Height);
            using (Font f = new Font("Segoe UI", 46f * u, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 214, 64)))
                g.DrawString("STARTA OM APPEN!", f, b, new RectangleF(br.X, br.Y + 20f * u, br.Width, 100f * u), cf);
            using (Font f = new Font("Segoe UI", 22f * u, FontStyle.Bold))
                g.DrawString("En ny version" + (updNew > 0 ? " (v" + updNew + ")" : "") + " har hämtats.\nTryck Esc och öppna appen igen.",
                    f, Brushes.White, new RectangleF(br.X + 20f * u, br.Y + 125f * u, br.Width - 40f * u, 110f * u), cf);
        }

        if (askName)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(244, 14, 18, 4)))
                g.FillRectangle(b, 0, 0, ClientSize.Width, ClientSize.Height);
            float ncx = ClientSize.Width / 2f, ncy = ClientSize.Height / 2f;
            using (Font f = new Font("Segoe UI", 40f * u, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 214, 64)))
                g.DrawString("VÄLJ DITT NAMN", f, b, new RectangleF(0, ncy - 220f * u, ClientSize.Width, 80f * u), cf);
            using (Font f = new Font("Segoe UI", 15f * u))
                g.DrawString("Namnet syns för andra spelare.\nAnvänd ett smeknamn, inte ditt riktiga namn.", f, Brushes.White,
                    new RectangleF(0, ncy - 135f * u, ClientSize.Width, 70f * u), cf);
            RectangleF nbox = new RectangleF(ncx - 220f * u, ncy - 40f * u, 440f * u, 64f * u);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 30, 38, 10))) g.FillRectangle(b, nbox);
            using (Pen p = new Pen(Color.FromArgb(255, 233, 242, 122), 3f * u)) g.DrawRectangle(p, nbox.X, nbox.Y, nbox.Width, nbox.Height);
            string shown = nameInput + (((tickN / 15) % 2 == 0) ? "|" : "");
            using (Font f = new Font("Segoe UI", 28f * u, FontStyle.Bold))
                g.DrawString(shown, f, Brushes.White, nbox, cf);
            using (Font f = new Font("Segoe UI", 12f * u))
            {
                g.DrawString("2-16 tecken. Skriv ditt namn och tryck Enter.", f, SB(Color.FromArgb(200, 233, 242, 122)),
                    new RectangleF(0, ncy + 40f * u, ClientSize.Width, 30f * u), cf);
                if (nameError.Length > 0)
                    g.DrawString(nameError, f, SB(Color.FromArgb(255, 255, 120, 100)),
                        new RectangleF(0, ncy + 75f * u, ClientSize.Width, 30f * u), cf);
            }
        }
    }

    // ---------- automatisk uppdatering ----------
    // Version för den .exe som byggdes från den här filen. Höj den när du skickar ut en ny version
    // (samma nummer som i version.txt på webben).
    const int BaseVersion = 11;
    string updText = "";
    string updStatus = "";
    int updNew = 0;

    static int MyVersion()
    {
        try
        {
            string n = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            int k = n.LastIndexOf("-v");
            int v;
            if (k >= 0 && int.TryParse(n.Substring(k + 2), out v)) return v;
        }
        catch { }
        return BaseVersion;
    }

    static string UpdDir
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Path.Combine("RuttetAgg", "updates")); }
    }

    static string UpdateUrl()
    {
        try
        {
            string saved = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Path.Combine("RuttetAgg", "updateurl.txt"));
            string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uppdateringsadress.txt");
            string u = null;
            if (File.Exists(local))
            {
                u = File.ReadAllText(local).Trim();
                if (u.Length > 0)
                {
                    Directory.CreateDirectory(UpdDir);
                    File.WriteAllText(saved, u);
                }
            }
            if ((u == null || u.Length == 0) && File.Exists(saved)) u = File.ReadAllText(saved).Trim();
            if (u == null || !u.StartsWith("https://") || u.Contains("DITT-NAMN")) return null;
            if (!u.EndsWith("/")) u += "/";
            return u;
        }
        catch { return null; }
    }

    static int NewestInstalled(out string path)
    {
        path = null;
        int best = 0;
        try
        {
            if (!Directory.Exists(UpdDir)) return 0;
            foreach (string f in Directory.GetFiles(UpdDir, "RuttetAgg-v*.exe"))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                int v;
                if (int.TryParse(n.Substring(n.LastIndexOf("-v") + 2), out v) && v > best) { best = v; path = f; }
            }
        }
        catch { }
        return best;
    }

    // Startar en nyare, redan hämtad version (om det finns någon) och avslutar den här.
    static bool LaunchNewer()
    {
        try
        {
            string p;
            int v = NewestInstalled(out p);
            if (p != null && v > MyVersion())
            {
                Process.Start(p);
                return true;
            }
        }
        catch { }
        return false;
    }

    static string FindCsc()
    {
        string w = Environment.GetEnvironmentVariable("windir");
        if (w == null) return null;
        string[] c = {
            Path.Combine(w, @"Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
            Path.Combine(w, @"Microsoft.NET\Framework\v4.0.30319\csc.exe") };
        foreach (string s in c) if (File.Exists(s)) return s;
        return null;
    }

    static string Hash(string t)
    {
        using (System.Security.Cryptography.SHA1 sh = System.Security.Cryptography.SHA1.Create())
        {
            byte[] h = sh.ComputeHash(new UTF8Encoding(false).GetBytes(t));
            StringBuilder sb = new StringBuilder();
            foreach (byte x in h) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }

    // Kollar efter uppdatering direkt vid start och sedan var 2:a minut medan appen är öppen.
    void StartUpdateCheck()
    {
        Thread th = new Thread(delegate () {
            while (true)
            {
                try { CheckUpdate(); }
                catch (Exception ex) { LogError(ex); updStatus = "Uppdateringsfel: " + ex.Message; }
                if (updText.Length > 0) break;
                Thread.Sleep(120000);
            }
        });
        th.IsBackground = true;
        th.Start();
    }

    void CheckUpdate()
    {
        if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ingen-uppdatering.txt")))
        {
            updStatus = "Uppdateringar avstängda (ingen-uppdatering.txt finns)";
            return;
        }
        string url = UpdateUrl();
        if (url == null)
        {
            updStatus = "Ingen uppdateringsadress - kolla uppdateringsadress.txt";
            return;
        }
        int mine = MyVersion();
        updStatus = "Söker uppdatering...";

        // städa gamla versioner (de som körs går inte att ta bort - det är okej)
        try
        {
            foreach (string f in Directory.GetFiles(UpdDir, "RuttetAgg-v*.exe"))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                int v;
                if (int.TryParse(n.Substring(n.LastIndexOf("-v") + 2), out v) && v < mine)
                {
                    try { File.Delete(f); } catch { }
                }
            }
        }
        catch { }

        try { ServicePointManager.SecurityProtocol = (SecurityProtocolType)(3072 | 768); } catch { }
        string stamp = DateTime.UtcNow.Ticks.ToString();
        using (WebClient wc = new WebClient())
        {
            string vtxt = wc.DownloadString(url + "version.txt?t=" + stamp).Trim();
            int remote;
            if (!int.TryParse(vtxt, out remote))
            {
                updStatus = "version.txt innehåller inte ett nummer";
                return;
            }
            if (remote <= mine) { updStatus = "Senaste versionen (v" + mine + ")"; return; }

            string exe = Path.Combine(UpdDir, "RuttetAgg-v" + remote + ".exe");
            if (File.Exists(exe))
            {
                updNew = remote;
                updText = "Ny version (v" + remote + ") finns - starta om appen!";
                updStatus = "";
                return;
            }
            Directory.CreateDirectory(UpdDir);
            string failed = Path.Combine(UpdDir, "failed-" + remote + ".txt");
            string csc = FindCsc();
            if (csc == null) { updStatus = "Hittar inte Windows byggverktyg (csc.exe)"; return; }

            updStatus = "Hämtar v" + remote + "...";
            byte[] data = wc.DownloadData(url + "RuttetAgg.cs?t=" + stamp);
            string text = new UTF8Encoding(false).GetString(data);
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);
            string hash = Hash(text);

            // har exakt den här koden redan misslyckats? Då försöker vi inte igen förrän koden ändras.
            if (File.Exists(failed))
            {
                string first = "";
                try { first = File.ReadAllLines(failed)[0]; } catch { }
                if (first == hash)
                {
                    updStatus = "Uppdatering v" + remote + " gick inte att bygga - se " + failed;
                    return;
                }
            }

            updStatus = "Bygger v" + remote + "...";
            string src = Path.Combine(UpdDir, "src-v" + remote + ".cs");
            string tmp = Path.Combine(UpdDir, "build-v" + remote + ".tmp");
            File.WriteAllText(src, text, new UTF8Encoding(true));

            ProcessStartInfo psi = new ProcessStartInfo(csc,
                "/nologo /target:winexe /out:\"" + tmp + "\" /r:System.Windows.Forms.dll /r:System.Drawing.dll \"" + src + "\"");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            bool ok = false;
            string output = "";
            using (Process pr = Process.Start(psi))
            {
                output = pr.StandardOutput.ReadToEnd();
                pr.WaitForExit();
                ok = (pr.ExitCode == 0) && File.Exists(tmp);
            }
            if (ok)
            {
                File.Move(tmp, exe);
                try { File.Delete(src); } catch { }
                try { File.Delete(failed); } catch { }
                updNew = remote;
                updText = "Ny version (v" + remote + ") hämtad - starta om appen!";
                updStatus = "";
            }
            else
            {
                File.WriteAllText(failed, hash + "\r\n" + output);
                try { File.Delete(tmp); } catch { }
                updStatus = "Uppdatering v" + remote + " misslyckades - se " + failed;
            }
        }
    }

    [STAThread]
    static void Main()
    {
        if (LaunchNewer()) return;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += delegate (object snd, ThreadExceptionEventArgs ev) { LogError(ev.Exception); };
        Application.EnableVisualStyles();
        Application.Run(new EggForm());
    }
}
