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
class Item { public string Name; public int Price, Slot, Lvl, Mode; public Color L, D; }
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
    string[] tabNames = { "FÄRG", "HUVUD", "ÖGON", "MUN", "EXTRA" };
    List<Item> items = new List<Item>();
    bool[] owned;
    int[] equipped = { 0, -1, -1, -1, -1 };
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

        int[] cnt = new int[5];
        foreach (Item it in items) cnt[it.Slot]++;
        foreach (int c in cnt) if (c > cap) cap = c;
        owned = new bool[items.Count];
    }

    void ItemColors(Item it, out Color l, out Color d)
    {
        float tt = (float)((Math.Sin(ang * 2) + 1) / 2);
        if (it.Mode == 1) { l = Hsv(ang * 40, 0.4, 1.0); d = Hsv(ang * 40 + 40, 0.9, 0.75); }
        else if (it.Mode == 2) { l = Lerp(RGB(150, 255, 80), RGB(255, 100, 220), tt); d = Lerp(RGB(0, 160, 60), RGB(150, 0, 180), tt); }
        else if (it.Mode == 3) { l = Lerp(RGB(230, 250, 255), RGB(160, 230, 255), tt); d = Lerp(RGB(90, 190, 230), RGB(200, 240, 255), tt); }
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
            string txt = "coins=" + coins + "\r\nowned=" + o + "\r\neq=" + eq +
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
            }
            for (int i = 0; i < owned.Length; i++) owned[i] = (i < o.Length && o[i] == '1');
            owned[0] = true;
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
    RectangleF[] shopTabs = new RectangleF[5];
    List<ShopRow> shopRows = new List<ShopRow>();

    void BuildShop()
    {
        float u = UI();
        float pw = 310 * u, px = ClientSize.Width - pw - 20 * u, py = 80 * u;
        shopRows.Clear();
        shopRebirth = new RectangleF(px + 10 * u, py + 44 * u, pw - 20 * u, 48 * u);
        float tw = (pw - 20 * u) / 5f;
        for (int i = 0; i < 5; i++)
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
        for (int i = 0; i < 5; i++)
            if (shopTabs[i].Contains(e.X, e.Y)) hoverTab = i;
        hoverRebirth = shopRebirth.Contains(e.X, e.Y);
        float vx = VX(e.X), vy = VY(e.Y);
        Cursor = (hover >= 0 || hoverTab >= 0 || hoverRebirth || btn.Contains(vx, vy) || OverEgg(vx, vy)) ? Cursors.Hand : Cursors.Default;
        if (btn.Contains(vx, vy)) Dodge();
    }

    void OnDown(object s, MouseEventArgs e)
    {
        BuildShop();
        for (int i = 0; i < 5; i++)
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
        if (OverEgg(vx, vy))
        {
            msg = "AJ! Sluta peta på det!";
            level += 12;
            Sound(0);
        }
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
        int reward = (int)Math.Round((double)(10f * Mult()));
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
        for (int i = 0; i < 5; i++)
        {
            bool act = (tab == i);
            Color tf = act ? Color.FromArgb(182, 214, 58) : (hoverTab == i ? Color.FromArgb(200, 60, 75, 16) : Color.FromArgb(170, 25, 32, 8));
            using (SolidBrush b = new SolidBrush(tf)) g.FillRectangle(b, shopTabs[i]);
            using (Font f = new Font("Segoe UI", 8.5f * u, FontStyle.Bold))
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
            using (Font f = new Font("Segoe UI", 11f * u))
                g.DrawString(it.Name, f, locked ? SB(Color.FromArgb(150, 150, 150, 150)) : Brushes.White,
                    new RectangleF(tx, r.R.Y, r.R.Width - (tx - r.R.X) - 70 * u, r.R.Height), lf);

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
                    g.DrawString(eq ? "PÅ" : "ÄGD", f, eq ? SB(yel) : SB(Color.FromArgb(190, 200, 200, 200)),
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

        using (Font f = new Font("Segoe UI", 9))
        using (SolidBrush b = new SolidBrush(Color.FromArgb(130, yel)))
            g.DrawString("Klicka på ägget för att poppa det snabbare.   (Esc = avsluta)   v" + MyVersion(), f, b,
                new RectangleF(0, ClientSize.Height - 28, ClientSize.Width, 20), sf);
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
    const int BaseVersion = 3;
    string updText = "";
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

    void StartUpdateCheck()
    {
        Thread th = new Thread(delegate () {
            try { CheckUpdate(); }
            catch (Exception ex) { LogError(ex); }
        });
        th.IsBackground = true;
        th.Start();
    }

    void CheckUpdate()
    {
        if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ingen-uppdatering.txt"))) return;
        string url = UpdateUrl();
        if (url == null) return;
        int mine = MyVersion();

        // städa gamla versioner (de som körs går inte att ta bort - det är okej)
        try
        {
            string old;
            foreach (string f in Directory.GetFiles(UpdDir, "RuttetAgg-v*.exe"))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                int v;
                if (int.TryParse(n.Substring(n.LastIndexOf("-v") + 2), out v) && v < mine)
                {
                    try { File.Delete(f); } catch { }
                }
            }
            old = null;
        }
        catch { }

        try { ServicePointManager.SecurityProtocol = (SecurityProtocolType)(3072 | 768); } catch { }
        string stamp = DateTime.UtcNow.Ticks.ToString();
        using (WebClient wc = new WebClient())
        {
            string vtxt = wc.DownloadString(url + "version.txt?t=" + stamp).Trim();
            int remote;
            if (!int.TryParse(vtxt, out remote) || remote <= mine) return;

            string exe = Path.Combine(UpdDir, "RuttetAgg-v" + remote + ".exe");
            if (File.Exists(exe)) { updText = "Ny version (v" + remote + ") finns - starta om appen!"; updNew = remote; return; }
            Directory.CreateDirectory(UpdDir);
            string failed = Path.Combine(UpdDir, "failed-" + remote + ".txt");
            if (File.Exists(failed)) return;

            string csc = FindCsc();
            if (csc == null) return;

            byte[] data = wc.DownloadData(url + "RuttetAgg.cs?t=" + stamp);
            string text = new UTF8Encoding(false).GetString(data);
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);
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
                updText = "Ny version (v" + remote + ") hämtad - starta om appen!";
                updNew = remote;
            }
            else
            {
                File.WriteAllText(failed, output);
                try { File.Delete(tmp); } catch { }
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
