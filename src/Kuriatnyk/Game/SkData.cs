using System;
using System.Collections.Generic;
using System.Linq;

namespace Kuriatnyk;

/// <summary>Оружие режима Soul Knight: своё, отдельно от стволов сюжета. Выпадает из сундуков в подземелье
/// и продаётся у торговца за пивчики. stam — стамина за выстрел (у ближнего и стартовых — 0).</summary>
public static class SKW
{
    public static readonly Weap[] LIST =
    {
        // --- ближний бой ---
        new(){id="stick",skr="common",nameRu="Деревянная палка",special="melee",rate=.4,dmg=32,meleeR=80,arc=1.15,sfx="swish",descRu="Ближний бой: бьёт всех перед собой и отбивает яйца"},
        new(){id="fork",skr="common",nameRu="Вилы",special="melee",rate=.45,dmg=38,meleeR=118,arc=.42,sfx="swish",descRu="Длинный укол вперёд: протыкает всех на линии"},
        new(){id="bat",skr="common",nameRu="Бита",special="melee",rate=.5,dmg=40,meleeR=86,arc=1.3,knock=3.2,sfx="swish",descRu="Отправляет врагов в полёт"},
        new(){id="axe",skr="rare",nameRu="Топор",special="melee",rate=.72,dmg=85,meleeR=96,arc=1.25,knock=2,stam=1,sfx="swish",descRu="Тяжёлый широкий удар"},
        new(){id="pan",skr="rare",nameRu="Сковородка",special="melee",rate=.42,dmg=48,meleeR=84,arc=1.3,stun=.5,reflect=true,stam=.5,sfx="swish",descRu="Оглушает и отбивает яйца обратно во врагов"},
        new(){id="scythe",skr="epic",nameRu="Коса",special="melee",rate=.5,dmg=70,meleeR=124,arc=2.4,stam=2,sfx="swish",descRu="Огромный взмах почти вокруг себя"},
        new(){id="sword",skr="legendary",nameRu="Меч-кладенец",special="melee",rate=.45,dmg=90,meleeR=104,arc=1.4,wave=60,stam=3,sfx="swish",descRu="Каждый взмах выпускает сияющую волну, которая режет всех на пути"},
        // --- стрелковое ---
        new(){id="sling",skr="common",nameRu="Рогатка",rate=.32,dmg=20,spd=620,life=.7,spread=.04,shape="pebble",sfx="pop",kick=1,descRu="Стартовое оружие: не тратит стамину"},
        new(){id="water",skr="common",nameRu="Водный пистолет",rate=.12,dmg=8,spd=560,life=.55,spread=.08,knock=3.5,shape="water",sfx="pop",kick=.5,descRu="Слабый, но отталкивает врагов. Стамину не тратит"},
        new(){id="peas",skr="common",nameRu="Горохострел",rate=.1,dmg=10,spd=700,life=.6,spread=.1,stam=.3,shape="pea",sfx="smg",kick=.8,descRu="Очереди гороха"},
        new(){id="seeds",skr="common",nameRu="Семечкомёт",rate=.65,dmg=8,pellets=7,spd=560,life=.4,spread=.5,stam=2,shape="seed",sfx="shotgun",kick=4,descRu="Веер семечек вблизи"},
        new(){id="honey",skr="rare",nameRu="Медомёт",rate=.18,dmg=14,spd=600,life=.65,spread=.06,honey=1.5,stam=.6,shape="honey",sfx="pop",kick=1,descRu="Липкий мёд замедляет врагов"},
        new(){id="plunger",skr="rare",nameRu="Вантуз-какашкомёт",rate=.5,dmg=30,spd=520,life=.7,spread=.03,zone="poop",zoneR=52,zoneT=3,zoneDps=8,stam=2,shape="poop",sfx="pop",kick=2,descRu="Стреляет какашками: где упала — вонючая лужа, враги в ней вязнут и чахнут"},
        new(){id="snow",skr="rare",nameRu="Снежкомёт",rate=.28,dmg=24,spd=640,life=.7,spread=.04,chill=1.6,stam=1,shape="snow",sfx="ice",kick=1,descRu="Снежки замедляют, а 4 попадания подряд замораживают"},
        new(){id="bubble",skr="rare",nameRu="Пузырятор",rate=.35,dmg=18,spd=380,life=1.1,spread=.08,stun=1.2,size=9,stam=1.5,shape="bubble",sfx="pop",kick=.5,descRu="Пузыри запирают врагов внутри на секунду"},
        new(){id="carrot",skr="rare",nameRu="Морковная снайперка",rate=.85,dmg=95,spd=1150,life=.9,spread=.01,pierce=2,stam=3,shape="carrot",sfx="shot",kick=3,descRu="Морковка прошивает троих"},
        new(){id="boomer",skr="rare",nameRu="Бумеранг",rate=.6,dmg=45,spd=620,life=1,boomer=true,pierce=99,stam=1.5,shape="boomer",sfx="swish",kick=0,descRu="Летит туда и обратно, задевая всех дважды"},
        new(){id="laser",skr="rare",nameRu="Лазерная указка",special="beam",rate=.12,dmg=12,stam=.5,sfx="laser",kick=0,descRu="Тонкий луч прожигает всех на линии"},
        new(){id="cactus",skr="rare",nameRu="Кактус-пушка",rate=.55,dmg=16,spd=560,life=.5,ring=10,stam=2.5,shape="spike",sfx="pop",kick=1,descRu="Иголки разлетаются во все стороны"},
        new(){id="teapot",skr="rare",nameRu="Чайник-паровоз",rate=.07,dmg=7,spd=420,life=.4,spread=.45,pierce=99,knock=1.6,stam=.35,shape="steam",sfx="flame",kick=.2,descRu="Струя пара: обжигает и отталкивает"},
        new(){id="pumpkin",skr="epic",nameRu="Тыквомёт",special="lob",rate=.9,dmg=95,boomR=86,stam=4,sfx="throw",kick=3,descRu="Навесом кидает взрывные тыквы"},
        new(){id="rocket",skr="epic",nameRu="Петардомёт",rate=.55,dmg=40,spd=480,life=1.4,hom=4,boomR=62,boomDmg=40,stam=3,shape="rocket",sfx="throw",kick=2,descRu="Самонаводящиеся петарды взрываются"},
        new(){id="bees",skr="epic",nameRu="Улей",rate=.4,dmg=12,pellets=3,spd=420,life=1.6,spread=.7,hom=6,stam=2.5,shape="bee",sfx="pop",kick=.5,descRu="Пчёлы сами находят врагов"},
        new(){id="accordion",skr="epic",nameRu="Баян",rate=.5,dmg=22,pellets=3,spd=430,life=.55,spread=.5,pierce=99,size=14,stam=3,shape="note",sfx="pop",kick=1,descRu="Звуковые волны проходят сквозь всех"},
        new(){id="gauss",skr="legendary",nameRu="Гаусс-пушка",rate=1,dmg=140,spd=1700,life=.9,pierce=99,ghost=true,knock=4,stam=7,shape="slug",sfx="rail",kick=7,descRu="Магнитный снаряд пробивает врагов и даже укрытия"},
        new(){id="grinder",skr="legendary",nameRu="Мясорубка",rate=.05,dmg=13,spd=680,life=.6,spread=.16,spinUp=true,stam=.45,shape="meat",sfx="smg",kick=.8,descRu="Раскручивается и строчит фрикадельками"},
        new(){id="coingun",skr="legendary",nameRu="Пивчикомёт",rate=.3,dmg=75,spd=760,life=.8,pierce=1,coin=true,shape="coin",sfx="shot",kick=2.5,descRu="Стреляет пивчиками: огромный урон, но каждые 3 выстрела стоят 1 пивчик"},
        // --- посохи ---
        new(){id="sfire",skr="rare",nameRu="Посох огня",rate=.6,dmg=35,spd=520,life=.9,boomR=60,boomDmg=30,burnT=2.5,stam=3,shape="fireorb",sfx="throw",kick=1,descRu="Огненный шар взрывается и поджигает"},
        new(){id="sice",skr="rare",nameRu="Посох льда",rate=.4,dmg=30,spd=700,life=.8,split=3,chill=1.6,stam=2,shape="shard",sfx="ice",kick=1,descRu="Ледышка раскалывается на три осколка"},
        new(){id="sheal",skr="rare",nameRu="Посох знахаря",rate=.3,dmg=22,spd=650,life=.8,heal=1.2,stam=1.5,shape="leaf",sfx="pop",kick=.5,descRu="Каждое попадание лечит тебя"},
        new(){id="spoison",skr="rare",nameRu="Посох болотника",rate=.8,dmg=20,spd=480,life=.8,zone="poison",zoneR=70,zoneT=4,zoneDps=12,stam=3,shape="poison",sfx="throw",kick=1,descRu="Шар лопается ядовитым облаком"},
        new(){id="sbolt",skr="epic",nameRu="Посох молний",special="chain",rate=.55,dmg=34,chain=5,stam=3.5,sfx="laser",kick=1,descRu="Молния перескакивает между пятью врагами"},
        new(){id="sroot",skr="epic",nameRu="Посох леса",rate=.75,dmg=30,spd=560,life=.8,zone="roots",zoneR=76,zoneT=2.5,zoneDps=10,stam=4,shape="seedling",sfx="throw",kick=1,descRu="Из земли вырастают корни и держат врагов"},
        new(){id="swind",skr="epic",nameRu="Посох ветра",rate=1.1,dmg=25,spd=260,life=1.6,pierce=99,size=18,stam=4.5,shape="tornado",sfx="swish",kick=1,descRu="Смерч затягивает врагов и кружит их"},
        new(){id="sstar",skr="legendary",nameRu="Посох звездочёта",rate=.45,dmg=30,pellets=3,spd=560,life=1.4,spread=.9,hom=7,stam=3,shape="star",sfx="pop",kick=1,descRu="Три звезды сами летят во врагов"},
    };
    public static readonly string[] IDS = LIST.Select(w => w.id).ToArray();
    /// <summary>С чем начинается забег: рогатка и палка, обе без стамины.</summary>
    public static readonly string[] START = { "sling", "stick" };
    /// <summary>Цены у торговца и в автомате на 0-1; чем глубже уровень, тем дороже (+15% за уровень).
    /// Автомат дешевле торговца: он выдаёт наугад одно зелье — здоровья или стамины.</summary>
    public const int STAM_PRICE = 10, HEAL_PRICE = 12, VEND_PRICE = 8;
    public static double LvMul(int lv) => 1 + .15 * (Math.Max(1, lv) - 1);
    public static int Cost(double basePrice, int lv) => Math.Max(5, (int)(Math.Round(basePrice * LvMul(lv) / 5) * 5));
    /// <summary>Сила ствола: урон в секунду с поправками на площадь, пробивание, самонаведение и эффекты.</summary>
    public static double Power(Weap w)
    {
        double per = w.dmg * Math.Max(1, w.pellets) * (w.ring > 0 ? w.ring * .45 : 1);
        if (w.special == "chain") per = w.dmg * w.chain * .7;
        if (w.boomR > 0) per += (w.boomDmg > 0 ? w.boomDmg : w.dmg) * 1.4;
        double dps = per / (w.spinUp ? w.rate * 2.2 : w.rate), k = 1;
        if (w.special == "melee") k *= w.meleeR / 90 * (.7 + w.arc * .35);
        if (w.pierce >= 99 || w.special == "beam" || w.ghost) k *= 1.5; else if (w.pierce > 0) k *= 1 + .2 * w.pierce;
        if (w.boomer) k *= 1.6; if (w.hom > 0) k *= 1.15; if (w.chill > 0 || w.honey > 0 || w.stun > 0) k *= 1.15;
        if (w.zone != null) k *= 1.25; if (w.burnT > 0) k *= 1.15; if (w.split > 0) k *= 1.3; if (w.heal > 0) k *= 1.25; if (w.reflect) k *= 1.15; if (w.knock >= 3) k *= 1.1;
        if (w.wave > 0) dps += w.wave / w.rate;
        return dps * k;
    }
    /// <summary>Цена ствола: основа по редкости плюс сила (корень, чтобы мощные не улетали в небо), с наценкой за уровень.</summary>
    public static int Price(Weap w, int lv = 1)
    {
        double b = w.skr switch { "common" => 4, "rare" => 14, "epic" => 28, _ => 46 };
        return Cost(b + Math.Sqrt(Power(w)) * 2.6, lv);
    }
    static readonly HashSet<string> STAFFS = new() { "sfire", "sice", "sheal", "spoison", "sbolt", "sroot", "swind", "sstar" };
    /// <summary>Раздел справочника: melee — ближний бой, staff — посохи, gun — остальное.</summary>
    public static string Group(Weap w) => w.special == "melee" ? "melee" : STAFFS.Contains(w.id) ? "staff" : "gun";
}

/// <summary>Пассивки забега Soul Knight: выбираются в портале между уровнями.</summary>
public sealed class SkPass { public string id, nameRu, descRu, icon; public bool once; public string name => L.T(nameRu); public string desc => L.T(descRu); }
public static class SKP
{
    public static readonly SkPass[] LIST =
    {
        new(){id="bag",nameRu="Рюкзак",descRu="+1 место для оружия",once=true,icon="bag"},
        new(){id="hp",nameRu="Крепкие перья",descRu="+25 к максимуму здоровья и лечение на 25",icon="heart"},
        new(){id="dmg",nameRu="Острый клюв",descRu="+15% урона",icon="beak"},
        new(){id="rate",nameRu="Быстрые лапки",descRu="+12% темпа стрельбы",icon="rate"},
        new(){id="spd",nameRu="Лёгкий пух",descRu="+10% скорости бега",icon="boot"},
        new(){id="stam",nameRu="Выносливость",descRu="+30 к максимуму стамины",icon="stam"},
        new(){id="sregen",nameRu="Бодрость",descRu="Кристаллы и зелья стамины дают вдвое больше",icon="stam"},
        new(){id="thrift",nameRu="Экономия",descRu="Оружие тратит на 25% меньше стамины",icon="stam"},
        new(){id="armor",nameRu="Скорлупа",descRu="−12% входящего урона",icon="shield"},
        new(){id="vamp",nameRu="Аппетит",descRu="Каждое убийство лечит 2 здоровья",icon="heart"},
        new(){id="magnet",nameRu="Магнит",descRu="Пивчики летят к тебе издалека, и их падает на 30% больше",icon="piv"},
        new(){id="crit",nameRu="Меткость",descRu="12% шанс крита ×2",icon="beak"},
        new(){id="melee",nameRu="Боевой задор",descRu="+40% урона ближним оружием",icon="sword"},
        new(){id="regen",nameRu="Заживление",descRu="Восстанавливает 1,5 здоровья в секунду, если 4 с не получал урона",icon="heart"},
        new(){id="ult",nameRu="Боевой дух",descRu="Ульта копится на 30% быстрее",icon="star"},
        new(){id="luck",nameRu="Удача",descRu="В сундуках и у торговца чаще редкое оружие",icon="clover"},
    };
    public static SkPass Get(string id) => LIST.FirstOrDefault(p => p.id == id);
}

/// <summary>Ульты статуй в святилище Soul Knight. Поклонился статуе — твоя ульта меняется на её до конца забега:
/// это ульта любого героя обычной игры (её может применить кто угодно) или «Подкрепление» — вызов бойца на подмогу.</summary>
public static class SKU
{
    public const string SUMMON = "summon";
    public const double SUMMON_T = 20; // сколько секунд сражается вызванный боец
    public static readonly string[] ALL = { "hen", "chick", "rooster", "duck", "goose", "quail", "guinea", "nurse", "adren", SUMMON };
    public static string Name(string id) => id == SUMMON ? L.T("Подкрепление") : D.HEROES[id].ult[0];
    public static string Desc(string id) => id == SUMMON ? L.T("Зовёт на подмогу случайного бойца: 20 секунд он сражается рядом") : D.HEROES[id].ult[1];
    /// <summary>Цвет камня-самоцвета на постаменте.</summary>
    public static string Gem(string id) => id switch
    {
        "hen" => "#ffc93a", "chick" => "#ffe066", "rooster" => "#ff6b4a", "duck" => "#7fd4ff", "goose" => "#cfe8ff",
        "quail" => "#8f7cff", "guinea" => "#86bcdc", "nurse" => "#9cf27a", "adren" => "#ff7a1a", _ => "#5fcf3f",
    };
}
