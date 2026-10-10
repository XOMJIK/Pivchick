using System;
using System.Collections.Generic;
using System.Linq;

namespace Kuriatnyk;

// Каталог: редкости, герои, оружие, враги, главы сюжета (порт web/js/data.js).
// Имена полей как в JS, чтобы игровой код переносился один в один.
// Строки хранятся по-русски, перевод — через свойства с L.T (как applyLang в веб-версии).

public sealed class Rar
{
    public string nameRu, c; public double w, card, need, cost; public double[] chance;
    public string name => L.T(nameRu);
}

public sealed class Hero
{
    public string id, cls, hat, nameRu, rar, sp, special, body, wing, tail, head, perkRu;
    public double[] chance;
    public double hp, spd, r, dmg, rate, armor, regen; public int nade;
    public string[] ultRu, p5Ru, p10Ru;
    public string name => L.T(nameRu);
    public string perk => L.T(perkRu);
    public string[] ult => new[] { L.T(ultRu[0]), L.T(ultRu[1]) };
    public string[] p5 => new[] { L.T(p5Ru[0]), L.T(p5Ru[1]) };
    public string[] p10 => new[] { L.T(p10Ru[0]), L.T(p10Ru[1]) };
}

public sealed class Weap
{
    public string id, cls, rar, nameRu, sfx, special, descRu, metaRu;
    public int price, eggs, pellets = 1, pierce, bounce;
    public double rate, dmg, spd, spread, life, kick, heal;
    public double ammo = double.PositiveInfinity;
    // Soul Knight: редкость (common…legendary, как у героев), стамина за выстрел и особенности снаряда
    public string skr, shape, zone, art;
    public double stam, meleeR, arc, stun, knock = 1, hom, chill, burnT, honey, poisT, boomR, boomDmg, zoneR, zoneT, zoneDps, split, size, wave;
    public int ring, chain; public bool boomer, ghost, spinUp, coin, reflect;
    public string name => L.T(nameRu);
    public string desc => L.T(descRu);
    public string meta => metaRu == null ? null : L.T(metaRu);
}

public sealed class Cls { public string nameRu, c, descRu, start; public string name => L.T(nameRu); public string desc => L.T(descRu); }

public sealed class ET
{
    public string id, nameRu, bul, tauntRu;
    public bool hop, fly, boss;
    public double hp, r, spd, score, coin, tok, dmg, bspd, range, keep, melee, dash, s;
    public double[] cd;
    public string[][] cols;
    public string[] pats, summon;
    public string name => L.T(nameRu);
    public string taunt => tauntRu == null ? null : L.T(tauntRu);
}

public sealed class Goal { public string type, what, boss; public int n; public double t; }

public sealed class Level
{
    public int gid, ch, i, coins, tokens, eggs; public string nameRu, textRu; public Goal goal;
    public Dictionary<string, double> mix; public bool night; public double diff;
    public string name => L.T(nameRu);
    public string text => L.T(textRu);
}

public sealed class Chapter
{
    public string nameRu, introRu, outroRu; public List<Level> levels = new();
    public string name => L.T(nameRu); public string intro => L.T(introRu); public string outro => L.T(outroRu);
}

public sealed class Patch { public string v, tag; public string[] itemsRu; }

public static class D
{
    public static readonly Dictionary<string, Rar> RAR = new()
    {
        ["common"] = new() { nameRu = "Обычный", c = "#8f9ba6", w = 1, card = 1, need = 1, cost = 1, chance = new[] { 12.0, 30, 60, 80 } },
        ["rare"] = new() { nameRu = "Редкий", c = "#2f95d0", w = .75, card = .7, need = .7, cost = 1.25, chance = new[] { 10.0, 25, 45, 70 } },
        ["epic"] = new() { nameRu = "Эпический", c = "#8b4fd8", w = .5, card = .45, need = .5, cost = 1.5, chance = new[] { 3.0, 9, 22, 45 } },
        ["legendary"] = new() { nameRu = "Легендарный", c = "#f08c00", w = .35, card = .3, need = .35, cost = 2, chance = new[] { .6, 3, 10, 30 } },
    };
    public static readonly string[] RAR_ORDER = { "legendary", "epic", "rare", "common" };

    public static readonly Dictionary<string, Hero> HEROES = new[]
    {
        new Hero{id="hen",cls="assault",hat="bandana",nameRu="Пеструшка",rar="common",chance=null,sp="hen",hp=100,spd=205,r=15,dmg=1,rate=1,armor=0,regen=0,nade=1,body="#fbfbf4",wing="#e6e3d6",
            perkRu="Сбалансированная, берёт в бой лишнее яйцо",
            ultRu=new[]{"Яичный дождь","Одна зона там, где врагов гуще всего: в неё падают 8 яиц-бомб"},
            p5Ru=new[]{"Запасливая","Каждая новая волна даёт +1 яйцо-гранату, а в Soul Knight — +15 стамины"},
            p10Ru=new[]{"Наседка","Кукуруза лечит вдвое сильнее и падает в полтора раза чаще"}},
        new Hero{id="chick",cls="sniper",hat="propeller",nameRu="Цыпа Шустрик",rar="common",chance=new[]{12.0,30},sp="chick",hp=75,spd=248,r=12,dmg=1,rate=1.25,armor=0,regen=0,nade=0,body="#ffd84a",wing="#f0b628",
            perkRu="Мелкий и быстрый, +25% темпа стрельбы",
            ultRu=new[]{"Турбо-пух","4 секунды двойной скорости и двойного темпа стрельбы"},
            p5Ru=new[]{"Увёртливый","15% шанс, что яйцо пролетит мимо"},
            p10Ru=new[]{"Пух-реактор","Ульта копится на 40% быстрее"}},
        new Hero{id="rooster",cls="tank",hat="cowboy",nameRu="Петя Шпора",rar="rare",chance=new[]{6.0,16},sp="rooster",hp=130,spd=192,r=16,dmg=1.15,rate=1,armor=0,regen=0,nade=0,body="#d9622b",wing="#9e3f1b",tail="#1f4a35",
            perkRu="Крепкий задира, +15% урона",
            ultRu=new[]{"Боевой клич","«КУКАРЕКУ!» оглушает всех рядом на 2,5 с, бьёт на 45, и 4 с Петя получает вдвое меньше урона"},
            p5Ru=new[]{"Шпоры","Кто кусает Петю, сам получает 20 урона"},
            p10Ru=new[]{"Задира","15% шанс критического удара ×2"}},
        new Hero{id="duck",cls="medic",hat="sailor",nameRu="Утка Кря",rar="rare",chance=new[]{6.0,16},sp="duck",hp=110,spd=200,r=15,dmg=1,rate=1,armor=0,regen=3,nade=0,body="#b9a68a",wing="#8a7558",head="#2f7d4a",
            perkRu="Плавает: переходит реки вплавь (в воде медленнее) и лечится сама, если 4 секунды не получала урон",
            ultRu=new[]{"Кря-волна","Лечит половину здоровья себе и бойцам рядом, сбивает яйца, отталкивает врагов и оставляет лужу, в которой враги вязнут"},
            p5Ru=new[]{"Утиный нырок","В воде утка ныряет: пули и яйца пролетают над ней"},
            p10Ru=new[]{"Второе дыхание","Один раз за бой встаёт с 50% здоровья"}},
        new Hero{id="goose",cls="tank",hat="visor",nameRu="Гусь Бронебой",rar="epic",chance=new[]{2.0,7},sp="goose",special="armor",hp=160,spd=178,r=17,dmg=1.05,rate=1,armor=.25,regen=0,nade=0,body="#a9aba3",wing="#7b7d76",head="#45474b",
            perkRu="Толстые перья: на 25% меньше урона",
            ultRu=new[]{"Гусиная крепость","5 секунд неуязвимости, вражеские яйца отлетают обратно, а в конце — ударная волна на 70"},
            p5Ru=new[]{"Шипение","Враги рядом с гусём двигаются на 30% медленнее"},
            p10Ru=new[]{"Бронеперья","Ещё −15% получаемого урона"}},
        new Hero{id="quail",cls="sniper",hat="ninja",nameRu="Перепёлка Тень",rar="rare",chance=new[]{1.0,1},sp="chick",special="quail",hp=80,spd=235,r=12,dmg=1.15,rate=1,armor=0,regen=0,nade=0,body="#a7835a",wing="#7a5a3a",head="#a7835a",
            perkRu="Тихая и меткая: +15% урона",
            ultRu=new[]{"Теневой выстрел","Три пробивающих луча в самых крепких врагов, а потом 3 с невидимости: враги теряют Перепёлку из виду"},
            p5Ru=new[]{"Зоркий глаз","+25% урона по целям дальше 250"},
            p10Ru=new[]{"Охотница","Убийство издалека заряжает ульту на 10%"}},
        new Hero{id="guinea",cls="assault",hat="goggles",nameRu="Цесарка Буря",rar="epic",chance=new[]{1.0,1},sp="hen",special="guinea",hp=115,spd=212,r=15,dmg=1.05,rate=1.05,armor=0,regen=0,nade=0,body="#4a4f5c",wing="#383c47",head="#86bcdc",
            perkRu="Пёстрая и шустрая: каждый 8-й выстрел — веер из 5 острых перьев",
            ultRu=new[]{"Перьевой ураган","4 с вихрь затягивает врагов к Цесарке, бьёт их и сбивает пули, а в конце раскидывает взрывом"},
            p5Ru=new[]{"Пёстрые перья","10% шанс увернуться от любого урона"},
            p10Ru=new[]{"Неугомонная","+15% темпа стрельбы"}},
        new Hero{id="nurse",cls="medic",hat="none",nameRu="Гусыня Медсестра",rar="rare",chance=new[]{1.0,1},sp="goose",special="nurse",hp=135,spd=190,r=16,dmg=1,rate=1,armor=.1,regen=0,nade=0,body="#fbfbf7",wing="#dcdcd4",
            perkRu="Аптечки: каждые 15 с бросает аптечку (+25 здоровья) самому раненому из своих",
            ultRu=new[]{"Полевой госпиталь","6 секунд зона лечения: лечит всех своих и сразу поднимает выбитых бойцов"},
            p5Ru=new[]{"Перевязка","Выбитые бойцы отряда возвращаются через 9 с вместо 18"},
            p10Ru=new[]{"Милосердие","Лечение класса «Медик» вдвое сильнее"}},
        new Hero{id="adren",cls="assault",hat="crown",nameRu="Боевой Петушок Adrenaline",rar="legendary",chance=new[]{.5,3},sp="rooster",special="adren",hp=140,spd=218,r=16,dmg=1.2,rate=1.15,armor=0,regen=0,nade=1,body="#2a2a32",wing="#c0392b",tail="#ff7a1a",
            perkRu="Легенда двора: каждый 5-й выстрел — огненный снаряд, который взрывается и поджигает врагов",
            ultRu=new[]{"Адреналин","6 с ярости: огненные кольца пуль, +40% скорости, а каждый убитый враг взрывается огнём"},
            p5Ru=new[]{"Кровь кипит","+30% урона, когда здоровья меньше половины"},
            p10Ru=new[]{"Неудержимый","Убийства лечат 3 здоровья, ярость длится на 2 с дольше"}},
    }.ToDictionary(h => h.id);
    public static readonly string[] HERO_IDS = HEROES.Keys.ToArray();

    public static readonly int[] NEED = { 0, 2, 4, 8, 12, 18, 25, 35, 45, 60 };
    public static readonly int[] COST = { 0, 20, 35, 60, 90, 130, 180, 250, 330, 450 };
    public static int needCards(string id, int l) => (int)Math.Ceiling(NEED[l] * RAR[HEROES[id].rar].need);
    public static int needCoins(string id, int l) => (int)(Math.Round(COST[l] * RAR[HEROES[id].rar].cost / 5, MidpointRounding.AwayFromZero) * 5);

    public sealed class Skin { public string nameRu, body, wing; public int price, eggs; public string name => L.T(nameRu); }
    public static readonly Dictionary<string, Skin> HATS = new()
    {
        ["none"] = new() { nameRu = "Без шапки" }, ["bandana"] = new() { nameRu = "Бандана" }, ["helmet"] = new() { nameRu = "Каска", price = 120 },
        ["cowboy"] = new() { nameRu = "Ковбойская шляпа", price = 200 }, ["pirate"] = new() { nameRu = "Пират", price = 250 }, ["ninja"] = new() { nameRu = "Ниндзя", price = 300 },
        ["crown"] = new() { nameRu = "Корона", price = 700 }, ["halo"] = new() { nameRu = "Нимб", eggs = 50 },
    };
    public static readonly Dictionary<string, Skin> COLORS = new()
    {
        ["native"] = new() { nameRu = "Родной окрас" }, ["pink"] = new() { nameRu = "Розовый", price = 150, body = "#f7a8c4", wing = "#e27aa3" },
        ["mint"] = new() { nameRu = "Мятный", price = 180, body = "#a8e6cf", wing = "#6cc3a0" }, ["night"] = new() { nameRu = "Ночной", price = 300, body = "#4a4f63", wing = "#2f3240" },
        ["gold"] = new() { nameRu = "Золотое перо", price = 800, body = "#f6cd4c", wing = "#d9a21c" }, ["platinum"] = new() { nameRu = "Платиновый", eggs = 70, body = "#e3e9f0", wing = "#a9b6c6" },
    };

    const double INF = double.PositiveInfinity;
    public static readonly Dictionary<string, Weap> WEAP = new[]
    {
        new Weap{id="pistol",cls="assault",rar="common",nameRu="Пугач",price=0,rate=.26,dmg=24,spd=640,spread=.05,pellets=1,life=.75,sfx="shot",kick=2,descRu="Надёжный стартовый ствол"},
        new Weap{id="sheriff",cls="sniper",rar="common",nameRu="Шериф",price=250,rate=.42,dmg=48,spd=820,spread=.02,pellets=1,life=.8,sfx="shot",kick=3.5,descRu="Медленный, но бьёт вдвое сильнее"},
        new Weap{id="millet",cls="assault",rar="common",nameRu="Пшеномёт",price=450,rate=.11,dmg=14,spd=680,spread=.15,pellets=1,life=.6,sfx="smg",kick=1.2,descRu="Поливает двор очередями"},
        new Weap{id="sawed",cls="tank",rar="common",nameRu="Обрез",price=650,rate=.6,dmg=14,spd=540,spread=.42,pellets=5,life=.38,sfx="shotgun",kick=5,descRu="Разносит всё вблизи"},
        new Weap{id="popcorn",cls="assault",rar="rare",nameRu="Попкорн-бластер",price=1000,rate=.2,dmg=22,spd=620,spread=.05,pellets=1,life=.8,sfx="pop",kick=2,pierce=2,descRu="Зерно прошивает троих, а при попадании лопается на 3 осколка"},
        new Weap{id="freeze",cls="tank",rar="rare",nameRu="Морозилка",price=1200,rate=.1,dmg=10,spd=720,spread=.06,pellets=1,life=.6,sfx="ice",kick=.8,special="ice",descRu="Ледяные иглы замедляют врагов вдвое, а 4 попадания подряд замораживают на 1,2 с",metaRu="Урон 10 · 10 выстр/с · замедление 50% · заморозка"},
        new Weap{id="flame",cls="tank",rar="legendary",nameRu="Огнемёт «Жар-птица»",price=1400,rate=.05,dmg=5,spd=450,spread=.34,pellets=1,life=.6,sfx="flame",kick=.3,special="flame",descRu="Струя огня поджигает всех врагов перед тобой, а горящие при смерти поджигают соседей",metaRu="Урон 5 × 20/с + горение 9/с · дальность средняя"},
        new Weap{id="minigun",cls="assault",rar="legendary",nameRu="Миниган «Молотилка»",price=1800,rate=.045,dmg=11,spd=760,spread=.17,pellets=1,life=.62,sfx="smg",kick=.8,special="mini",descRu="Раскручивается до бешеного темпа, но на бегу тормозит",metaRu="Урон 11 · до 22 выстр/с после раскрутки"},
        new Weap{id="mortar",cls="medic",rar="legendary",nameRu="Яйцемёт",price=2000,rate=.85,dmg=65,spd=0,spread=0,pellets=1,life=0,sfx="throw",kick=3,special="mortar",heal=2,descRu="Навесом кидает взрывные яйца, каждый задетый враг лечит своих",metaRu="Взрыв 65 по площади · +2 здоровья за врага"},
        new Weap{id="granny",cls="tank",rar="common",nameRu="Бабушкин дробовик",price=0,rate=.75,dmg=13,spd=540,spread=.4,pellets=4,life=.42,sfx="shotgun",kick=4,descRu="Старый, но надёжный дробовик танка"},
        new Weap{id="cornrifle",cls="sniper",rar="common",nameRu="Кукурузная винтовка",price=0,rate=.75,dmg=58,spd=980,spread=.01,pellets=1,life=.9,sfx="shot",kick=3,pierce=1,descRu="Меткий дальний выстрел, прошивает двоих"},
        new Weap{id="crossbow",cls="sniper",rar="rare",nameRu="Арбалет «Клюв»",price=900,rate=.85,dmg=85,spd=1000,spread=.01,pellets=1,life=1,sfx="shot",kick=2.5,pierce=3,descRu="Тяжёлый болт пробивает четверых и пригвождает их на полсекунды"},
        new Weap{id="syringe",cls="medic",rar="common",nameRu="Шприцемёт",price=0,rate=.24,dmg=17,spd=700,spread=.04,pellets=1,life=.7,sfx="pop",kick=1,heal=1,descRu="Каждое попадание лечит тебя и бойца рядом",metaRu="Урон 17 · 4 выстр/с · +1 здоровья за попадание"},
        new Weap{id="vitamin",cls="medic",rar="rare",nameRu="Витаминный пулемёт",price=800,rate=.1,dmg=10,spd=720,spread=.12,pellets=1,life=.62,sfx="smg",kick=.8,heal=.5,descRu="Очереди витаминок: бьют врагов и лечат своих",metaRu="Урон 10 · 10 выстр/с · +0,5 здоровья за попадание"},
        new Weap{id="rail",cls="sniper",rar="legendary",nameRu="Рельсотрон",eggs=120,rate=1.05,dmg=130,spd=0,spread=0,pellets=1,life=0,sfx="rail",kick=7,special="rail",descRu="Луч прошивает всех врагов и даже укрытия на линии",metaRu="Урон 130 всем на линии · раз в секунду"},
        new Weap{id="turbocat",cls="assault",rar="legendary",nameRu="Пулемёт Турбокота",price=2400,rate=.07,dmg=12,spd=760,spread=.11,pellets=1,life=.62,sfx="smg",kick=.9,special="cat",descRu="Мурчит очередями, а каждый 6-й выстрел — турбо-клубок",metaRu="Урон 12 · 14 выстр/с · каждый 6-й — клубок ×3, пробивает двоих"},
        new Weap{id="figure",cls="tank",rar="rare",nameRu="Дробовик Фигурки",price=1100,rate=.72,dmg=13,spd=560,spread=.46,pellets=6,life=.5,sfx="shotgun",kick=4.5,special="fig",bounce=1,descRu="Стреляет звёздочками, сердечками и кубиками — они отскакивают от укрытий",metaRu="Урон 13×6 · дробинки-фигурки рикошетят один раз"},
        new Weap{id="shotgun",nameRu="Дробовик",rate=.68,dmg=15,spd=560,spread=.36,pellets=6,life=.42,ammo=14,sfx="shotgun",kick=6},
        new Weap{id="smg",nameRu="Тарахтелка",rate=.085,dmg=13,spd=720,spread=.13,pellets=1,life=.62,ammo=80,sfx="smg",kick=1.2},
    }.ToDictionary(w => w.id);

    public static readonly Dictionary<string, Cls> CLASSES = new()
    {
        ["assault"] = new() { nameRu = "Штурмовик", c = "#e8742a", descRu = "+10% скорости и темпа стрельбы", start = "pistol" },
        ["tank"] = new() { nameRu = "Танк", c = "#6f7782", descRu = "+25% здоровья и −10% входящего урона, но на 8% медленнее", start = "granny" },
        ["sniper"] = new() { nameRu = "Снайпер", c = "#2f95d0", descRu = "+20% урона и пули летят на 35% дальше, но −10% здоровья", start = "cornrifle" },
        ["medic"] = new() { nameRu = "Медик", c = "#4fae36", descRu = "Лечится на 1,2 в секунду, лечит всех своих рядом и вдвое быстрее поднимает выбитых бойцов", start = "syringe" },
    };
    public static readonly string[] CLASS_ORDER = { "assault", "tank", "sniper", "medic" };
    public static readonly string[] PRIMARY = { "pistol", "millet", "popcorn", "minigun", "turbocat", "granny", "sawed", "figure", "freeze", "flame", "cornrifle", "sheriff", "crossbow", "rail", "syringe", "vitamin", "mortar" };
    public static string clsOf(string heroId) => HEROES[heroId].cls;

    // редкость оружия и прокачка картами: +6% урона за уровень, максимум 10
    public static readonly Dictionary<string, Rar> WRAR = new()
    {
        ["common"] = new() { nameRu = "Обычное", c = "#8f9ba6", w = 1, card = 1, need = 1, cost = 1 },
        ["rare"] = new() { nameRu = "Редкое", c = "#2f95d0", w = .7, card = .65, need = .7, cost = 1.3 },
        ["legendary"] = new() { nameRu = "Легендарное", c = "#f08c00", w = .4, card = .35, need = .4, cost = 1.8 },
    };
    public static readonly string[] WRAR_ORDER = { "common", "rare", "legendary" };
    public static readonly int[] WNEED = { 0, 3, 5, 8, 12, 16, 22, 30, 40, 52 };
    public static readonly int[] WCOST = { 0, 30, 50, 80, 120, 170, 230, 300, 390, 500 };
    public static int wNeed(string id, int l) => (int)Math.Ceiling(WNEED[l] * WRAR[WEAP[id].rar].need);
    public static int wCost(string id, int l) => (int)(Math.Round(WCOST[l] * WRAR[WEAP[id].rar].cost / 5, MidpointRounding.AwayFromZero) * 5);
    public const double W_STEP = .06;
    public const double NIGHT_R = 250;

    static string[][] HC => new[] { new[] { "#a8632f", "#7d4520" }, new[] { "#4b4744", "#2e2b29" }, new[] { "#dba948", "#b8862c" } };
    public static readonly Dictionary<string, ET> ETYPE = new[]
    {
        new ET{id="hen",nameRu="Несушка",hop=true,hp=45,r=15,spd=95,score=100,coin=3,tok=1,cd=new[]{1.3,2.3},dmg=8,bspd=290,range=440,keep=220,cols=HC},
        new ET{id="fox",nameRu="Лиса",hp=55,r=15,spd=168,score=120,coin=4,tok=1,melee=13},
        new ET{id="turkey",nameRu="Индюк",hp=150,r=22,spd=68,score=250,coin=9,tok=2,cd=new[]{2,2.8},dmg=7,bspd=250,range=400,melee=15},
        new ET{id="crow",nameRu="Ворона",hp=35,r=13,spd=150,score=130,coin=4,tok=1,cd=new[]{2.4,3.4},dmg=15,fly=true},
        new ET{id="raccoon",nameRu="Енот",hp=70,r=15,spd=115,score=160,coin=6,tok=2,dash=520,melee=18},
        new ET{id="rat",nameRu="Мышь",hp=18,r=10,spd=178,score=40,coin=1,tok=0,melee=6},
        new ET{id="bigrat",nameRu="Крыса",hp=55,r=14,spd=228,score=140,coin=5,tok=1,melee=15,dash=620,cd=new[]{1.6,2.4}},
        new ET{id="wolf",nameRu="Волк",hp=75,r=16,spd=185,score=170,coin=5,tok=1,melee=14,dash=470},
        new ET{id="owl",nameRu="Сова",hp=55,r=14,spd=85,score=180,coin=5,tok=1,cd=new[]{2.6,3.6},dmg=16,bspd=640,bul="feather",keep=320,range=560},
        new ET{id="ferret",nameRu="Хорёк",hp=60,r=14,spd=120,score=150,coin=5,tok=1,cd=new[]{1.6,2.4},dmg=6,bspd=420,bul="pellet",keep=210,range=440},
        new ET{id="robohen",nameRu="Робо-несушка",hop=true,hp=110,r=15,spd=85,score=220,coin=7,tok=2,cd=new[]{2,2.8},dmg=14,bspd=720,bul="bolt",keep=240,range=520},
        new ET{id="mole",nameRu="Крот",hp=50,r=13,spd=105,score=140,coin=4,tok=1,cd=new[]{2.2,3.2},dmg=9,bspd=300,bul="dirt",melee=10,keep=160,range=420},
        new ET{id="kamibird",nameRu="Стриж-камикадзе",hp=24,r=11,spd=290,score=150,coin=4,tok=1,fly=true,s=1.3},
        new ET{id="eagle",nameRu="Орёл",hp=80,r=16,spd=170,score=220,coin=7,tok=2,fly=true,dash=580,melee=20,s=1.35,cd=new[]{2.6,3.6}},
        new ET{id="gturkey",nameRu="Индюк-генерал",hop=true,hp=1100,r=34,spd=62,score=3000,coin=60,tok=25,boss=true,melee=25,bspd=240,dmg=9,s=1.6,pats=new[]{"fan","burst","summon","dash"},summon=new[]{"hen","turkey"},dash=400,bul="egg",tauntRu="Ко мне, солдаты!"},
        new ET{id="ataman",nameRu="Лис-атаман",hp=2300,r=30,spd=120,score=6000,coin=120,tok=40,boss=true,melee=28,bspd=270,dmg=10,s=1.75,pats=new[]{"spiral","dash","summon","ring"},summon=new[]{"fox","raccoon"},dash=520,bul="fire",tauntRu="Банда, за мной!"},
        new ET{id="badger",nameRu="Барсук-Бульдозер",hp=3000,r=30,spd=80,score=7000,coin=150,tok=45,boss=true,melee=30,bspd=250,dmg=11,s=1.7,pats=new[]{"dash","fan","summon","dash","ring"},summon=new[]{"wolf","raccoon"},dash=480,bul="rock",tauntRu="Разойдись — снесу!"},
        new ET{id="wolfboss",nameRu="Вожак Серый Хвост",hp=4200,r=30,spd=140,score=9000,coin=180,tok=50,boss=true,melee=32,bspd=280,dmg=12,s=1.8,pats=new[]{"dash","summon","ring","dash","spiral"},summon=new[]{"wolf"},dash=600,bul="fire",tauntRu="Ау-у-у! Стая, ко мне!"},
        new ET{id="steelturkey",nameRu="Стальной Индюк",hop=true,hp=5500,r=34,spd=62,score=11000,coin=210,tok=55,boss=true,melee=34,bspd=300,dmg=13,s=1.7,pats=new[]{"rain","fan","summon","burst","ring"},summon=new[]{"robohen","ferret"},dash=380,bul="bolt",tauntRu="ПРОТОКОЛ «ОЩИПАТЬ» ЗАПУЩЕН"},
        new ET{id="drferret",nameRu="Доктор Хорёк",hp=7000,r=30,spd=115,score=14000,coin=250,tok=60,boss=true,melee=34,bspd=300,dmg=14,s=1.9,pats=new[]{"spiral","rain","summon","ring","dash"},summon=new[]{"ferret","robohen"},dash=520,bul="bolt",tauntRu="Мои творения, в атаку!"},
        new ET{id="eagleboss",nameRu="Орёл Буревестник",hp=8000,r=28,spd=165,score=17000,coin=290,tok=65,boss=true,fly=true,melee=36,bspd=320,dmg=15,s=2,pats=new[]{"dash","fan","summon","dash","rain"},summon=new[]{"crow","eagle"},dash=640,bul="feather",tauntRu="С неба не уйдёшь!"},
        new ET{id="emperor",nameRu="Император Ворон",hp=10000,r=30,spd=130,score=25000,coin=400,tok=80,boss=true,fly=true,melee=38,bspd=300,dmg=16,s=2.3,pats=new[]{"spiral","rain","summon","ring","dash","fan"},summon=new[]{"crow","owl","eagle"},dash=560,bul="fire",tauntRu="Склонитесь перед Императором!"},
    }.ToDictionary(e => e.id);

    public static readonly Dictionary<string, string> KILLN = new()
    {
        ["mole"] = "Кроты", ["any"] = "Враги", ["fox"] = "Лисы", ["crow"] = "Вороны", ["turkey"] = "Индюки", ["wolf"] = "Волки", ["owl"] = "Совы",
        ["robohen"] = "Робо-несушки", ["rat"] = "Мыши", ["bigrat"] = "Крысы", ["ferret"] = "Хорьки", ["eagle"] = "Орлы", ["raccoon"] = "Еноты", ["hen"] = "Несушки",
    };
    public static string killN(string k) => KILLN.TryGetValue(k, out var v) ? L.T(v) : null;

    static Dictionary<string, double> M(params (string, double)[] a) => a.ToDictionary(x => x.Item1, x => x.Item2);
    public static readonly Dictionary<string, double>[] CH_MIX =
    {
        M(("hen",1),("fox",.7),("turkey",.35),("crow",.4),("raccoon",.4),("rat",.4)),
        M(("wolf",1),("owl",.5),("fox",.6),("raccoon",.5),("crow",.3),("bigrat",.25)),
        M(("ferret",1),("robohen",.6),("rat",.5),("raccoon",.3),("bigrat",.45)),
        M(("crow",1),("eagle",.6),("owl",.5),("wolf",.4),("ferret",.3),("robohen",.3),("bigrat",.3)),
    };

    // [название, цель, состав врагов, текст, ночь]
    static readonly (string name, string introRu, string outroRu, object[][] lv)[] CH_RAW =
    {
        ("Курятник в беде",
         "Тихое утро на ферме кончилось: банда Лиса-атамана решила забрать наш курятник. Сначала он шлёт мелочь — но за ней придут и другие.",
         "Атаман повержен! Убегая, он прокричал: «Серый Хвост за меня отомстит!» Следы банды ведут в Тёмный лес.",
         new object[][]{
          new object[]{"Тревога в курятнике","w7","hen","Чужие несушки лезут через забор. Покажи им, кто тут главный."},
          new object[]{"Зерновой налёт","s180","rat,hen:.4","Крысы почуяли запах пшена. Не подпускай их к амбару три минуты."},
          new object[]{"Лисьи следы","k:fox:24","fox,hen:.4","У забора рыжая шерсть: лисы сделали подкоп. Прогони 24 лисы."},
          new object[]{"Пропавший цыплёнок","w7","hen,fox:.6","Наседки пересчитали цыплят — одного не хватает. Пробейся через семь волн и найди его."},
          new object[]{"Чёрные крылья","k:crow:24","crow,hen:.5","Вороны кидают яйца с неба. Уходи из красных кругов и сбей 24 вороны."},
          new object[]{"Индюшиный дозор","k:turkey:15","turkey,hen:.6","Индюки-разведчики осматривают двор. Не дай им вернуться к своему генералу."},
          new object[]{"Ночь у амбара","w7","raccoon,rat:.6","Ночью у амбара кто-то шуршит. Это еноты! Пройди семь волн в темноте.",1},
          new object[]{"Засада у колодца","s180","fox,hen,crow:.5","Цыплёнок нашёлся у колодца, но вокруг засада. Держись три минуты, пока его уводят домой."},
          new object[]{"Шпионки в перьях","k:any:75","hen,turkey:.4,fox:.5","Среди наших несушек затесались шпионки генерала. Победи 75 врагов."},
          new object[]{"Индюк-генерал","b:gturkey","hen,turkey:.35","Генерал лично привёл армию к нашему забору. Сбей с него фуражку!"},
          new object[]{"Прикрытие генерала","w7","turkey,hen","Генерал сбежал, но оставил прикрытие. Пройди семь волн."},
          new object[]{"Крысиный король","s180","rat,raccoon:.3","Крысы собрались в огромную стаю. Продержись три минуты."},
          new object[]{"Новый подкоп","k:fox:30","fox,rat:.4","Лисы роют ход прямо под насест. Прогони 30 лис."},
          new object[]{"Вороний патруль","w7","crow,hen:.6","Вороны кружат над двором с утра до вечера. Отбей семь волн."},
          new object[]{"Тёмный сарай","s180","raccoon,fox:.7","В сарае погас свет. Держись три минуты, пока Петя чинит фонарь.",1},
          new object[]{"Перехваченное письмо","k:any:90","","Ворона уронила письмо: «Атаман нападёт на рассвете». Готовься — победи 90 врагов."},
          new object[]{"Рассвет","w8","","Солнце встаёт, а вместе с ним — вся банда. Пройди восемь волн."},
          new object[]{"Последний забор","s180","","Забор трещит. Продержись три минуты, пока несушки ставят подпорки."},
          new object[]{"Перед бурей","k:any:105","","Ночь перед решающим боем. Банда идёт в последний натиск: победи 105 врагов.",1},
          new object[]{"Лис-атаман","b:ataman","fox,raccoon:.5,hen:.4","Главарь банды пришёл сам. Это решающий бой за курятник!"}}),
        ("Тёмный лес",
         "Следы банды ведут в Тёмный лес. Здесь правит волчья стая Серого Хвоста, а совы-снайперы сторожат каждую тропу.",
         "Вожак сдался и выдал тайну: банду снабжал Доктор Хорёк. На своей птицефабрике он строит армию роботов.",
         new object[][]{
          new object[]{"Опушка","w6","fox,raccoon:.6","Ты у края леса. Беглые лисы атамана прячутся за пнями."},
          new object[]{"Волчий вой","k:wolf:30","wolf,fox:.4","Из чащи слышен вой. Волчата проверяют твою храбрость — прогони 30."},
          new object[]{"Совиный глаз","k:owl:25","owl,fox:.4","Совы целятся издалека — перед выстрелом видна красная линия прицела. Сбей 25 сов."},
          new object[]{"Тропа енотов","s210","raccoon,wolf:.3","Еноты устроили засаду на узкой тропе. Продержись три с половиной минуты."},
          new object[]{"Ночная поляна","w6","wolf,owl:.5","Ночью в лесу темнее, чем на ферме. Пройди шесть волн.",1},
          new object[]{"Ягодник","k:any:100","","Ягодник сторожат часовые Серого Хвоста. Победи 100 врагов."},
          new object[]{"Старый дуб","s210","owl,crow:.6","К старому дубу слетелись совы и вороны. Держись три с половиной минуты."},
          new object[]{"Волчья тропа","w6","wolf,fox:.5","Волки идут по твоему следу. Отбей шесть волн."},
          new object[]{"Туманное болото","k:any:100","","На болоте туман и темень. Победи 100 врагов и не увязни.",1},
          new object[]{"Барсук-Бульдозер","b:badger","wolf,raccoon:.5","Барсук сносит деревья, как спички. Останови его!"},
          new object[]{"Сломанная плотина","w6","wolf,raccoon,bigrat:.5","Барсук сломал бобровую плотину — вода гонит врагов прямо на тебя."},
          new object[]{"Совиный совет","k:owl:25","owl,crow:.4","Совы собрались на тайный совет. Сорви его — сбей 25 сов."},
          new object[]{"Гнилой мост","s210","wolf,owl:.6","Мост скрипит под лапами. Продержись три с половиной минуты, пока цыплята переходят.",1},
          new object[]{"Лисья нора","k:fox:40","fox,wolf:.3","Здесь прячутся последние лисы атамана. Найди 40."},
          new object[]{"Запах логова","w7","wolf,owl:.5,crow:.4","Пахнет волком — логово близко. Пройди семь волн."},
          new object[]{"Полная луна","s210","wolf,owl:.3","В полнолуние волки злее. Держись три с половиной минуты.",1},
          new object[]{"Стражи логова","k:wolf:40","wolf,raccoon:.4","У входа в логово дежурят волки. Прогони 40."},
          new object[]{"Перекрёсток троп","w7","","Все тропы сходятся здесь — и все враги тоже."},
          new object[]{"Подозрительная тишина","k:any:110","","Лес затих. Это ловушка! Победи 110 врагов.",1},
          new object[]{"Вожак Серый Хвост","b:wolfboss","wolf,owl:.4","Серый Хвост вышел из логова. Победи вожака стаи!"}}),
        ("Птицефабрика доктора Хорька",
         "За лесом дымит труба птицефабрики. Доктор Хорёк собирает там робо-несушек и солдат-хорьков, чтобы захватить все курятники округи.",
         "Фабрика остановлена. Но в чертежах доктора стоит подпись: «Для Императора Ворона». Над горами собираются чёрные тучи.",
         new object[][]{
          new object[]{"Проходная","w6","ferret","Вход охраняют хорьки в касках. Прорвись через шесть волн."},
          new object[]{"Склад зерна","s210","rat,ferret:.5","Здесь хранят краденое зерно. Держись три с половиной минуты, пока цыплята уносят мешки."},
          new object[]{"Конвейер","k:robohen:30","robohen,ferret:.4","С конвейера сходят робо-несушки. Перед выстрелом они целятся красным лучом. Сломай 30."},
          new object[]{"Цех упаковки","w6","ferret,robohen:.5","Коробки, ленты и хорьки. Пройди шесть волн."},
          new object[]{"Аварийный свет","s210","ferret,rat:.6","Доктор вырубил свет на всём заводе. Держись три с половиной минуты в темноте.",1},
          new object[]{"Лаборатория","k:any:110","","В колбах — формула роботов. Победи 110 врагов и забери её."},
          new object[]{"Полигон","w6","robohen,ferret:.6","На полигоне испытывают новых роботов. На тебе."},
          new object[]{"Вентиляция","k:bigrat:40","bigrat,rat:.6,ferret:.3","Крысы-шпионы бегают по трубам — они крупнее мышей, быстрее и кусаются больно. Поймай 40."},
          new object[]{"Котельная","s210","","Жарко и шумно. Продержись три с половиной минуты у котлов."},
          new object[]{"Стальной Индюк","b:steelturkey","robohen,ferret:.6","Первый боевой робот фабрики. Разбери его на винтики!"},
          new object[]{"Склад запчастей","w7","robohen,ferret,bigrat:.4","Из запчастей собирают новых роботов. Помешай сборке."},
          new object[]{"Ночная смена","k:ferret:45","ferret,rat:.4","Заступила ночная смена хорьков. Прогони 45.",1},
          new object[]{"Сборочный цех","s210","robohen,ferret:.4","Роботы сходят с ленты потоком. Держись три с половиной минуты."},
          new object[]{"Пульт управления","k:robohen:35","robohen,ferret:.5","Сломай 35 роботов, чтобы пробиться к пульту."},
          new object[]{"Сирена","w7","","Сработала сирена — вся охрана бежит к тебе."},
          new object[]{"Кабинет доктора","k:any:130","","Доктор сбежал, оставив охрану. Победи 130 врагов."},
          new object[]{"Секретный лифт","s240","","Лифт едет медленно, а враги лезут со всех сторон.",1},
          new object[]{"Подземный ангар","w7","robohen,ferret,raccoon:.4","В ангаре стоит огромный боевой мех. Пробейся к нему."},
          new object[]{"Предохранители","k:any:130","","Выбей предохранители завода — победи 130 врагов."},
          new object[]{"Доктор Хорёк","b:drferret","ferret,robohen:.6","Доктор сам сел за рычаги. Финальная битва на фабрике!"}}),
        ("Гнездо Императора",
         "На вершине горы, в Чёрном Гнезде, сидит Император Ворон. Это он стоял за атаманом, волками и фабрикой. Пора подниматься в гору.",
         "Император повержен, Чёрное Гнездо пало! Курятники всей округи свободны, а петухи на рассвете поют о тебе песни. Конец… пока что.",
         new object[][]{
          new object[]{"Подножие горы","w6","crow,hen:.4","Горную тропу охраняют вороны Императора."},
          new object[]{"Горный ветер","k:eagle:30","eagle,crow:.4","Орлы пикируют сверху — перед броском они замирают. Сбей 30."},
          new object[]{"Каменная осыпь","s240","crow,eagle:.5","Сверху летят камни и яйца. Держись четыре минуты."},
          new object[]{"Пещера","w6","owl,wolf:.6","В пещере темно и сыро. Пройди шесть волн.",1},
          new object[]{"Перевал","k:any:130","","На перевале стоит застава Императора. Победи 130 врагов."},
          new object[]{"Старая мельница","s240","","Мельница скрипит на ветру. Держись четыре минуты."},
          new object[]{"Вороньи гнёзда","k:crow:50","crow,eagle:.3","Разори вороньи гнёзда — сбей 50 ворон."},
          new object[]{"Снежное поле","w7","eagle,wolf,fox:.5","Холодно и скользко. Пройди семь волн по снегу."},
          new object[]{"Ночной штурм","s240","","Враги идут в атаку под покровом ночи. Держись четыре минуты.",1},
          new object[]{"Орёл Буревестник","b:eagleboss","crow,eagle:.5","Генерал воздушных войск Императора. Спусти его на землю!"},
          new object[]{"Облачный мост","w7","crow,eagle,owl:.6","Мост из облаков… и из врагов."},
          new object[]{"Остатки банд","k:any:140","hen,fox,wolf,ferret,robohen,raccoon","Все, кто уцелел у атамана, волков и доктора, собрались здесь. Победи 140."},
          new object[]{"Буря","s240","eagle,crow","Гром и молнии. Держись четыре минуты.",1},
          new object[]{"Совиная стража","k:owl:35","owl,crow:.5","Совы-снайперы стерегут ворота. Сбей 35."},
          new object[]{"Ворота Гнезда","w7","","Ворота открываются. Пройди семь волн."},
          new object[]{"Зал трофеев","k:any:150","","Здесь висят украденные перья всех курятников. Победи 150 врагов."},
          new object[]{"Императорская гвардия","s240","","Гвардия бросилась в бой. Держись четыре минуты."},
          new object[]{"Тронный коридор","w7","","Коридор к трону тонет во тьме. Пройди семь волн.",1},
          new object[]{"Перед троном","k:any:150","","Император ждёт. Победи 150 стражей."},
          new object[]{"Император Ворон","b:emperor","crow,owl:.5,eagle:.5","Последний бой. Победи Императора — и все курятники станут свободными!"}}),
    };

    static Dictionary<string, double> parseMix(string str, int ch)
    {
        if (string.IsNullOrEmpty(str)) return new Dictionary<string, double>(CH_MIX[ch]);
        var m = new Dictionary<string, double>();
        foreach (var q in str.Split(','))
        {
            var kv = q.Split(':');
            m[kv[0]] = kv.Length > 1 ? double.Parse(kv[1], System.Globalization.CultureInfo.InvariantCulture) : 1;
        }
        return m;
    }
    static Goal parseGoal(string g)
    {
        if (g[0] == 'w') return new Goal { type = "waves", n = int.Parse(g[1..]) };
        if (g[0] == 's') return new Goal { type = "survive", t = int.Parse(g[1..]) };
        if (g[0] == 'k') { var q = g.Split(':'); return new Goal { type = "kill", what = q[1], n = int.Parse(q[2]) }; }
        return new Goal { type = "boss", boss = g.Split(':')[1] };
    }

    public static readonly List<Chapter> CHAPTERS = new();
    public static readonly List<Level> STORY = new();

    static D()
    {
        foreach (var w in SKW.LIST) WEAP[w.id] = w; // оружие Soul Knight живёт в общем каталоге, но только в этом режиме
        for (int ch = 0; ch < CH_RAW.Length; ch++)
        {
            var raw = CH_RAW[ch];
            var C = new Chapter { nameRu = raw.name, introRu = raw.introRu, outroRu = raw.outroRu };
            for (int i = 0; i < raw.lv.Length; i++)
            {
                var t = raw.lv[i];
                int gid = STORY.Count; var goal = parseGoal((string)t[1]); var mix = parseMix((string)t[2], ch); bool boss = goal.type == "boss";
                if (goal.type == "kill" && goal.what != "any" && !mix.ContainsKey(goal.what)) mix[goal.what] = 1;
                if ((ch >= 1 || i >= 10) && !mix.ContainsKey("kamibird")) mix["kamibird"] = ch == 3 ? .3 : ch == 0 ? .1 : .16; // стрижи-камикадзе — со второй половины первой главы
                var Lv = new Level
                {
                    gid = gid, ch = ch, i = i, nameRu = (string)t[0], goal = goal, mix = mix, textRu = (string)t[3], night = t.Length > 4,
                    diff = 1 + ch * 4 + i * .22,
                    coins = (int)Math.Round((40 + gid * 7) * (boss ? 1.6 : 1), MidpointRounding.AwayFromZero),
                    tokens = (int)Math.Round((18 + gid * 1.2) * (boss ? 1.5 : 1), MidpointRounding.AwayFromZero),
                    eggs = i == 19 ? 15 : i == 9 ? 8 : (i == 4 || i == 14) ? 4 : 2
                };
                STORY.Add(Lv); C.levels.Add(Lv);
            }
            CHAPTERS.Add(C);
        }
    }

    // патчноуты: показываются в окне «Об игре» (новые сверху)
    public static readonly Patch[] PATCH_NOTES =
    {
        new(){v="0.7.2",itemsRu=new[]{
            "Soul Knight: святилище со статуей птицы — поклонись ей за пивчики, и её ульта заменит твою до конца забега: ульта любого героя или новое «Подкрепление», которое зовёт бойца на подмогу. Чем сильнее ульта и глубже уровень, тем дороже поклон",
            "Soul Knight: мини-карта в углу экрана и значки взятых пассивок под ней",
            "Soul Knight: любое оружие ближнего боя отбивает яйца обратно во врагов и само замахивается, когда в героя летит снаряд",
            "Soul Knight: здоровье само не восстанавливается — ни между уровнями, ни от умений героев; пассивка «Запас яиц» заменена на «Знахаря»: кукуруза и зелья здоровья лечат вдвое сильнее",
            "Soul Knight: гранат больше нет; из ящиков падает стамина и лишь изредка кукуруза или пивчик",
            "Soul Knight: на уровне бывает не больше одного автомата — в стартовой комнате или у торговца; за пивчики он выдаёт одно зелье наугад: здоровья или стамины",
            "Soul Knight: у Пеструшки «Запасливая» вместо гранаты даёт +15 стамины за каждую волну",
            "Soul Knight: надпись на вывеске торговца больше не вылезает за край"}},
        new(){v="0.7.1",itemsRu=new[]{
            "Новый Легендарный сундук с огненным яйцом: больше всего карт и золотых яиц и самый высокий шанс на героя. Даётся за оба мира Soul Knight, 40-ю волну бесконечного боя и Императора Ворона или продаётся за 450 золотых яиц",
            "Открытие сундуков: список наград прокручивается, а кнопка «Забрать» всегда на виду",
            "Карточки героев, оружия и сундуков: значки и кнопка «Свойства» больше не закрывают картинку",
            "Soul Knight: цена оружия зависит от его силы и растёт с каждым уровнем, а пивчиков падает намного меньше — на крутой ствол придётся копить",
            "Soul Knight: стамина сама не восстанавливается — только синие кристаллы с врагов и зелья; кристаллы падают чаще",
            "Soul Knight: в стартовой комнате каждого уровня стоит автомат — один раз за пивчики выдаёт случайное оружие или зелья стамины и здоровья",
            "Soul Knight: торговец продаёт два ствола, зелье стамины и зелье здоровья",
            "Soul Knight: у оружия на земле видны характеристики и сравнение с тем, что в руках",
            "Пивчикомёт тратит 1 пивчик на 3 выстрела",
            "Сборки игры для Windows и Android — на странице GitHub Releases"}},
        new(){v="0.7",itemsRu=new[]{
            "Сюжет: каждый уровень длится не меньше трёх минут — цели стали больше, а если справишься раньше, придётся продержаться до конца. На уровнях с боссом он выходит через 1:15",
            "Новый враг — Стриж-камикадзе: быстрая птица с бомбой летит прямо на тебя и взрывается. Поворачивает плавно — можно увернуться в последний момент или сбить на подлёте",
            "Обычные враги больше не превращаются в камикадзе",
            "Враги под баффами: красная аура — сила (бьют больнее), зелёная — скорость, жёлтая — скорострельность. За них дают больше очков и зёрен",
            "Шанс крита ×2 у всех героев и бойцов отряда: 5%, у снайперов 12%",
            "Soul Knight: второй мир — Заводские катакомбы, уровни 1-1 … 1-5 с новыми врагами, Стальным Индюком в стороне и Доктором Хорьком в конце",
            "Бесконечный бой: больше зёрен и жетонов за волны и врагов, большой жетон каждые 5 волн, золотые яйца за волны 5–50, второй сверхбольшой сундук за 50-ю волну и бонус за достигнутую волну",
            "Стамина осталась только в режиме Soul Knight",
            "Пираний стало меньше, и они больше не кусают врагов",
            "Сверхбольшой сундук больше не гарантирует нового героя",
            "Кнопка выхода из игры в главном меню",
            "Новые языки: украинский, испанский, немецкий, португальский, польский и французский"}},
        new(){v="0.6",itemsRu=new[]{
            "Пираньи во всех реках: у берега, в воде и на мосту они выпрыгивают и кусают — героя, отряд и врагов. Перед прыжком видно «!», можно отбежать",
            "Soul Knight: на уровнях 0-2, 0-3 и 0-4 в некоторых комнатах течёт речка с мостами и пираньями",
            "Soul Knight: комнаты стали намного просторнее, враги появляются подальше от героя, мыши — по двое и вылезают дольше",
            "Soul Knight: стамина восстанавливается медленно и только когда не стреляешь — пополняй её синими кристаллами",
            "Деревянные ящики в сюжете, бесконечном бою и подземелье: разбиваются выстрелами, ударами и взрывами, внутри кукуруза, яйца, стамина, зёрна или пивчики"}},
        new(){v="0.5",itemsRu=new[]{
            "Soul Knight: 35 своих стволов — вантуз-какашкомёт, гаусс-пушка, топор, мясорубка, коса, меч-кладенец, бумеранг, улей, баян и 8 посохов. Стволов сюжета в подземелье больше нет",
            "Soul Knight: рюкзак на 2 ствола, оружие лежит на земле и берётся кнопкой «Взять» (E) или меняется на то, что в руках; 1, 2, 3 — выбор ствола",
            "Soul Knight: на каждом уровне лавка торговца и комната с сундуком, в котором случайное оружие",
            "Soul Knight: пивчики — монетки забега. Падают с врагов и за зачищенные комнаты, тратятся у торговца, остаток превращается в зёрна",
            "Soul Knight: в портале между уровнями — выбор одной из трёх пассивок; «Рюкзак» (+1 место для оружия) выпадает только один раз",
            "Стамина: выстрелы тратят её, она сама восстанавливается. В подземелье её 100 и без стамины стреляет только оружие, которое её не тратит; в сюжете и бесконечном бою — 50, без неё оружие стреляет вдвое реже. Изредка с врагов падают синие кристаллы стамины",
            "Герои: карточки стали компактнее, а характеристики, улучшение и таблица уровней — в окне «Свойства» с вкладками «Характеристики» и «Способности»",
            "Оружие переехало в отдельный раздел «Оружие» в меню; там же справочник всех стволов Soul Knight",
            "Магазин: каждый день две случайные пачки карт для твоих героев и стволов за зёрна",
            "Настройки: экран — оконный, без рамок или полный; F11 включает и выключает полный экран",
            "Настройки: «Управление» — свои клавиши для всех действий, у каждого две клавиши",
            "Оптимизация: поле целиком держится в памяти видеокарты, за кадром ничего не рисуется, меньше мусора в каждом кадре, а пока открыт раздел меню, поле под ним не перерисовывается"}},
        new(){v="0.4",itemsRu=new[]{
            "У рек появились притоки: на каждой карте от основного русла отходит рукав со своими мостами, в том числе гнилыми",
            "Враги ищут дорогу через мосты по всей сети рек, обходят изгибы берега и скользят вдоль воды, а не застревают",
            "Враги больше не появляются в реке",
            "Несушки, Робо-несушки и куриные боссы (Индюк-генерал, Стальной Индюк) перелетают воду — герой по-прежнему не может",
            "Новый враг — Крыса: крупнее мыши, быстрее, прыгает и больно кусает. Старые мелкие крысы теперь Мыши",
            "Яичный дождь (ульта Пеструшки и её же в отряде) больше не бьёт по каждому врагу: одна зона там, где врагов гуще всего",
            "Гуси больше не одинаковые: Бронебой — серый, со стальным нагрудником и наплечником; Медсестра — белая, с красным крестом на крыле, румянцем и ресницами",
            "Сундуки: кнопка «Свойства» — у каждого сундука свои шансы на героев, карты героев и карты оружия по редкостям",
            "Враги не залезают внутрь бойца — выстрел в упор больше не пролетает мимо",
            "Настройки: язык (русский или английский), громкость музыки и звуков, тряска экрана, автонаводка",
            "Английский язык: переведены меню, герои, оружие, враги, все 80 уровней сюжета и патчноуты",
            "Музыка: своя мелодия для меню, боя и боссов",
            "В паузе появились кнопки «Заново» и «Настройки»; кнопка автонаводки переехала в настройки",
            "Исправлено: после проигрыша и перезапуска больше не всплывает номер прошлой волны",
            "Обновлён список создателей",
            "Язык меняется сразу, без перезапуска — окно настроек и бой остаются на месте",
            "Музыка включается с первого касания, а не после захода в настройки",
            "У каждого героя свои способности, и чем реже герой, тем они интереснее: Утка плавает по рекам, Медсестра бросает аптечки, Петя после клича получает вдвое меньше урона, Перепёлка уходит в тень, Цесарка стреляет веером перьев и затягивает врагов в вихрь, Бронебой бьёт ударной волной, Adrenaline стреляет огненными снарядами",
            "Редкое и легендарное оружие стреляет интереснее: попкорн лопается на осколки, морозилка замораживает, арбалет пригвождает, огонь перекидывается на соседей",
            "Большой жетон теперь даётся за первое прохождение каждого уровня и за каждые 10 волн бесконечного боя",
            "Бесконечный бой: каждые 5 волн выходит босс",
            "Места в отряде продаются только за золотые яйца: 50, 100 и 250",
            "Сундуки можно открывать сразу по 3 и по 10",
            "У каждого героя свой фирменный образ; скины из магазина убраны, потраченное на них вернули",
            "Магазин: раздел «Золотые яйца» с наборами на 39, 79, 179, 309, 749 и 1999 яиц (пока закрыты) и обменом яиц на зёрна"}},
        new(){v="0.3",itemsRu=new[]{
            "Камикадзе: раненый враг иногда поджигает фитиль, бежит к тебе и взрывается. Если добить его пулями — он просто взорвётся на месте",
            "Пеньки — новое препятствие на картах",
            "Бомбардировка стала умнее: вороны и боссы бьют то по месту, то по направлению движения, то ковровой цепочкой",
            "Новые герои: Перепёлка Тень (снайпер), Цесарка Буря (штурмовик), Гусыня Медсестра (медик) — у каждой своя ульта и пассивки",
            "Отряд: каждому бойцу можно выбрать оружие своего класса, и бойцы сами применяют свои ульты",
            "Медики лечат всех своих в радиусе (игрока и весь отряд), лечение видно зелёными плюсиками; рядом с медиком выбитые бойцы встают вдвое быстрее",
            "Карточки отряда: кнопки «Сменить оружие» и «Убрать» больше не вылезают за край",
            "Баланс: враги бьют сильнее (×1,3 и +7% за ступень сложности), лечение медиков, кукуруза и лечение за волну слабее, предметы падают реже",
            "Баланс: камикадзе редкие (5%, не больше одного за раз и не чаще раза в 12 секунд), кроты больше не мешают появляться остальным врагам",
            "Враги реже переключаются на бойцов отряда — основной удар снова по герою",
            "Фон главного меню можно выбрать: кнопка «Фон» под бойцом, на выбор все 5 карт",
            "Кукуруза-лечилка падает на четверть реже",
            "Новое оружие: Пулемёт Турбокота (штурмовик, каждый 6-й выстрел — пробивающий клубок ×3) и Дробовик Фигурки (танк, звёздочки и сердечки рикошетят от укрытий)",
            "Редкость оружия: Обычное, Редкое, Легендарное",
            "Карты оружия падают из сундуков: улучшай купленные стволы в магазине до 10 уровня, +6% урона за уровень (и у бойцов отряда тоже)",
            "Новая система сундуков: Обычный, Большой и Сверхбольшой. Внутри — новые герои или карты к ним, карты оружия, зёрна и золотые яйца. Герои выпадают только из сундуков",
            "Сверхбольшой сундук: гарантированный новый герой, пока есть закрытые. Даётся за финального босса каждой главы и за 25-ю волну бесконечного боя, или за 200 золотых яиц"}},
        new(){v="0.2",itemsRu=new[]{
            "Реки на всех картах: по воде ходить нельзя, переходи по мостам. Гнилые мосты скрипят и рушатся после второго прохода",
            "5 карт со своей рекой: Двор (бесконечный бой), Ферма, Тёмный лес, Птицефабрика и Горы — по одной на главу",
            "Враги и отряд ищут дорогу через целые мосты",
            "Кроты: во время раунда вырываются ямки, из них вылезают кроты-шахтёры и кидаются землёй, а потом ямки зарываются",
            "Кнопка «Об игре»: создатели, версия и патчноуты; номер версии берётся из патчноутов",
            "Крупнее и удобнее кнопки: пауза, звук, граната, ульта, вкладки и кнопки в магазине"}},
        new(){v="0.1",itemsRu=new[]{
            "Сюжет: 4 главы по 20 уровней, 8 боссов, ночные уровни",
            "Бесконечный бой с волнами",
            "6 героев 4 редкостей, прокачка картами до 10 уровня, пассивки на 5 и 10, ульты",
            "Классы: Штурмовик, Танк, Снайпер, Медик — свои бонусы и своё оружие",
            "15 основных стволов, временные стволы и яйца-гранаты",
            "Отряд до 3 бойцов",
            "Сундуки, жетоны, большие жетоны, золотые яйца",
            "Магазин шапок, окрасов и оружия",
            "Лобби-меню; «Многопользовательская игра» и «Режим Soul Knight» пока закрыты"}},
    };
    public static readonly string[][] CREDITS = { new[] { "nutze", "Arthur Dyakonov", "креатор, геймдизайн, тестирование" }, new[] { "XOMJIK", "Illia Martseniuk", "креатор, кодер" } };
    // версия игры = верхняя запись патчноутов
    public static string GAME_VERSION => PATCH_NOTES[0].v;
    public static string GAME_TAG => PATCH_NOTES[0].tag ?? "";

    /* ---------- сундуки ---------- */
    public sealed class Chest
    {
        public string nameRu, subRu; public double[] coins, hcard, wcard, egg; public int hpacks, wpacks;
        public Dictionary<string, double> hb, wb;
        public string name => L.T(nameRu); public string sub => L.T(subRu);
    }
    public static readonly Chest[] CHESTS =
    {
        new(){nameRu="Обычный сундук",subRu="корзинка несушки",coins=new[]{25.0,50},hpacks=2,wpacks=1,hcard=new[]{3.0,6},wcard=new[]{3.0,6},egg=new[]{.08,1,3},
            hb=M(("common",1),("rare",1),("epic",.8),("legendary",.6)),wb=M(("common",1),("rare",.6),("legendary",.25))},
        new(){nameRu="Большой сундук",subRu="золотое яйцо",coins=new[]{150.0,250},hpacks=4,wpacks=2,hcard=new[]{8.0,14},wcard=new[]{8.0,14},egg=new[]{.3,2,5},
            hb=M(("common",1),("rare",1),("epic",1),("legendary",1)),wb=M(("common",1),("rare",.8),("legendary",.45))},
        new(){nameRu="Сверхбольшой сундук",subRu="аметистовое яйцо",coins=new[]{500.0,800},hpacks=6,wpacks=4,hcard=new[]{14.0,22},wcard=new[]{15.0,25},egg=new[]{1.0,5,12},
            hb=M(("common",.8),("rare",1),("epic",1.2),("legendary",1.5)),wb=M(("common",1),("rare",1),("legendary",.8))},
        new(){nameRu="Легендарный сундук",subRu="огненное яйцо",coins=new[]{1500.0,2500},hpacks=10,wpacks=6,hcard=new[]{22.0,34},wcard=new[]{24.0,36},egg=new[]{1.0,15,30},
            hb=M(("common",.6),("rare",1),("epic",1.4),("legendary",2)),wb=M(("common",.8),("rare",1),("legendary",1.2))},
    };
    public const int EGG_PRICE = 80, SUPER_PRICE = 200, LEG_PRICE = 450;

    public sealed class Pack { public string nameRu, tagRu; public int eggs, coins; public string name => L.T(nameRu); }
    public static readonly Pack[] GEM_PACKS =
    {
        new(){nameRu="Горстка яиц",eggs=39}, new(){nameRu="Корзинка яиц",eggs=79}, new(){nameRu="Лукошко яиц",eggs=179},
        new(){nameRu="Ящик яиц",eggs=309}, new(){nameRu="Телега яиц",eggs=749,tagRu="Хит"}, new(){nameRu="Золотой курятник",eggs=1999,tagRu="Выгодно"},
    };
    public static readonly Pack[] GRAIN_PACKS = { new() { nameRu = "Мешочек зерна", eggs = 10, coins = 500 }, new() { nameRu = "Мешок зерна", eggs = 50, coins = 2750 }, new() { nameRu = "Амбар зерна", eggs = 150, coins = 9000 } };
    public static readonly int[] SLOT_PRICE = { 50, 100, 250 }; // места в отряде — только за золотые яйца
}
