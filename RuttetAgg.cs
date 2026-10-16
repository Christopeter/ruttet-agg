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
class Part { public float X, Y, VX, VY, S; public int L, M; public Color C; }
class Floater { public float X, Y; public int L; public string T; public bool Coin; }
class Item { public string Name, Kind; public int Price, Slot, Lvl, Mode, Power, MaxDur, Bonus; public Color L, D; }
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
    string[] tabNames = { "FÄRG", "HUVUD", "ÖGON", "MUN", "EXTRA", "VERKTYG" };
    List<Item> items = new List<Item>();
    bool[] owned;
    int[] equipped = { 0, -1, -1, -1, -1, -1 };
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

        int[] cnt = new int[6];
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
        Load();
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
        if (e.KeyCode == Keys.Escape) Close();
        else if (e.KeyCode == Keys.Space)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (popTimer == 0 && eggScale > 0.5f)
                HitEgg(210f + (float)(rnd.NextDouble() * 50 - 25), 330f + (float)(rnd.NextDouble() * 70 - 35));
        }
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
            string txt = "coins=" + coins + "\r\nowned=" + o + "\r\neq=" + eq + "\r\ndur=" + du +
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

    void Load()
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
            string nDur = null;
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
    RectangleF[] shopTabs = new RectangleF[6];
    List<ShopRow> shopRows = new List<ShopRow>();

    void BuildShop()
    {
        float u = UI();
        float pw = 340 * u, px = ClientSize.Width - pw - 20 * u, py = 80 * u;
        shopRows.Clear();
        shopRebirth = new RectangleF(px + 10 * u, py + 44 * u, pw - 20 * u, 48 * u);
        float tw = (pw - 20 * u) / 6f;
        for (int i = 0; i < 6; i++)
            shopTabs[i] = new RectangleF(px + 10 * u + i * tw, py + 98 * u, tw - 2 * u, 30 * u);
        List<int> ids = new List<int>();
        for (int i = 0; i < items.Count; i++) if (items[i].Slot == tab) ids.Add(i);
        ids.Sort(delegate (int a, int b)
        {
            int c = items[a].Lvl.CompareTo(items[b].Lvl);
            if (c != 0) return c;
            return items[a].Price.CompareTo(items[b].Price);
        });
        float y = py + 134 * u;
        foreach (int i in ids)
        {
            ShopRow r = new ShopRow();
            r.Item = i;
            r.R = new RectangleF(px + 10 * u, y, pw - 20 * u, 38 * u);
            shopRows.Add(r);
            y += 42 * u;
        }
        shopPanel = new RectangleF(px, py, pw, (134 + cap * 42 + 8) * u);
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
        for (int i = 0; i < 6; i++)
            if (shopTabs[i].Contains(e.X, e.Y)) hoverTab = i;
        hoverRebirth = shopRebirth.Contains(e.X, e.Y);
        float vx = VX(e.X), vy = VY(e.Y);
        mx = e.X; my = e.Y;
        bool hand = (hover >= 0 || hoverTab >= 0 || hoverRebirth || btn.Contains(vx, vy) || OverEgg(vx, vy));
        if (equipped[5] >= 0 && !shopPanel.Contains(e.X, e.Y)) Cursor = Cursors.None;
        else Cursor = hand ? Cursors.Hand : Cursors.Default;
        if (btn.Contains(vx, vy)) Dodge();
    }

    void OnDown(object s, MouseEventArgs e)
    {
        BuildShop();
        for (int i = 0; i < 6; i++)
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
        int reward = (int)Math.Round((double)(10f * Mult() * (1f + toolBonus / 100f)));
        toolBonus = 0;
        coins += reward;
        pops++;
        level = 0;
        popTimer = 28;
        shake = 10;
        coinPulse = 1f;
        msg = "PLASK! +" + reward + " äggcoins";
        Color lc, dc;
        EggColors(out lc, out dc);
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
            default:
            {
                using (Pen p = new Pen(wc, 6)) g.DrawLine(p, 0, -20, 0, 70);
                break;
            }
        }
        g.Restore(st);
    }

    // ---------- rita: tillbehör ----------
    void DrawMouth(Graphics g)
    {
        if (equipped[3] < 0) return;
        string nm = items[equipped[3]].Name;
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
            g.DrawString("Nivå " + lvl + "   •   x" + Mult().ToString("0.0") + " multiplier", f, b, new RectangleF(0, 80, 420, 24), sf);

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
            DrawExtraBack(g);
            Rectangle er = new Rectangle(-80, -110, 160, 210);
            using (LinearGradientBrush lb = new LinearGradientBrush(er, lc, dc, 60f))
                g.FillEllipse(lb, er);
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
            using (SolidBrush b = new SolidBrush(Color.FromArgb(al, p.C)))
                g.FillEllipse(b, p.X - p.S / 2, p.Y - p.S / 2, p.S, p.S);
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
        for (int i = 0; i < 6; i++)
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
            using (Font f = new Font("Segoe UI", 11f * u))
                g.DrawString(it.Name, f, locked ? SB(Color.FromArgb(150, 150, 150, 150)) : Brushes.White,
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
    }

    // ---------- automatisk uppdatering ----------
    // Version för den .exe som byggdes från den här filen. Höj den när du skickar ut en ny version
    // (samma nummer som i version.txt på webben).
    const int BaseVersion = 7;
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
