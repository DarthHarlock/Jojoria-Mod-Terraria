using Terraria.Localization;

namespace Jojo.Content.Habilidades.Cinderella.Cinderella_Tier_4.ArmarioHabilida
{
    // Orden de las columnas de cada texto (NO cambiar el orden del enum):
    // Ingles, Español, Italiano, Frances, Chino Simplificado,
    // Portugues (Portugal), Polaco, Ruso, Portugues (Brasil), Aleman
    public enum Idioma
    {
        Ingles = 0,
        Espanol = 1,
        Italiano = 2,
        Frances = 3,
        ChinoSimplificado = 4,
        Portugues = 5,
        Polaco = 6,
        Ruso = 7,
        PortuguesBrasil = 8,
        Aleman = 9
    }

    public static class Traducciones
    {
        // Pon aqui un idioma para forzarlo (por ejemplo Idioma.Portugues para
        // Portugal). Dejalo en null para usar el idioma del juego.
        public static Idioma? Forzado = null;

        public static Idioma Actual
        {
            get
            {
                if (Forzado.HasValue)
                    return Forzado.Value;

                switch ((GameCulture.CultureName)Language.ActiveCulture.LegacyId)
                {
                    case GameCulture.CultureName.Spanish: return Idioma.Espanol;
                    case GameCulture.CultureName.Italian: return Idioma.Italiano;
                    case GameCulture.CultureName.French: return Idioma.Frances;
                    case GameCulture.CultureName.Chinese: return Idioma.ChinoSimplificado;
                    // El portugues de Terraria es el de Brasil (pt-BR)
                    case GameCulture.CultureName.Portuguese: return Idioma.PortuguesBrasil;
                    case GameCulture.CultureName.Polish: return Idioma.Polaco;
                    case GameCulture.CultureName.Russian: return Idioma.Ruso;
                    case GameCulture.CultureName.German: return Idioma.Aleman;
                    default: return Idioma.Ingles;
                }
            }
        }

        static string T(string[] textos) => textos[(int)Actual];

        //                                    EN                       ES                      IT                       FR                           ZH                 PT                           PL                    RU                    PT-BR                        DE
        public static string Titulo => T(new[] {
            "Cinderella's Wardrobe", "Armario de Cinderella", "Armadio di Cinderella", "Garde-robe de Cinderella", "Cinderella 的衣柜", "Guarda-fatos da Cinderella", "Szafa Cinderelli", "Гардероб Cinderella", "Guarda-roupa da Cinderella", "Cinderellas Kleiderschrank" });

        public static string Genero => T(new[] {
            "Gender", "Género", "Genere", "Genre", "性别", "Género", "Płeć", "Пол", "Gênero", "Geschlecht" });

        public static string Masculino => T(new[] {
            "Male", "Masculino", "Maschile", "Masculin", "男", "Masculino", "Mężczyzna", "Мужской", "Masculino", "Männlich" });

        public static string Femenino => T(new[] {
            "Female", "Femenino", "Femminile", "Féminin", "女", "Feminino", "Kobieta", "Женский", "Feminino", "Weiblich" });

        public static string Cambiar => T(new[] {
            "[Change]", "[Cambiar]", "[Cambia]", "[Changer]", "[更改]", "[Alterar]", "[Zmień]", "[Изменить]", "[Trocar]", "[Ändern]" });

        public static string Armadura => T(new[] {
            "Armor", "Armadura", "Armatura", "Armure", "盔甲", "Armadura", "Zbroja", "Броня", "Armadura", "Rüstung" });

        public static string Pelo => T(new[] {
            "Hair", "Pelo", "Capelli", "Cheveux", "发型", "Cabelo", "Fryzura", "Прическа", "Cabelo", "Frisur" });

        public static string ColorPelo => T(new[] {
            "Hair color", "Color de pelo", "Colore capelli", "Couleur cheveux", "发色", "Cor do cabelo", "Kolor włosów", "Цвет волос", "Cor do cabelo", "Haarfarbe" });

        public static string ColorPiel => T(new[] {
            "Skin color", "Color de piel", "Colore pelle", "Couleur peau", "肤色", "Cor da pele", "Kolor skóry", "Цвет кожи", "Cor da pele", "Hautfarbe" });

        public static string ColorOjos => T(new[] {
            "Eye color", "Color de ojos", "Colore occhi", "Couleur yeux", "瞳色", "Cor dos olhos", "Kolor oczu", "Цвет глаз", "Cor dos olhos", "Augenfarbe" });

        public static string ColorCamisa => T(new[] {
            "Shirt color", "Color camisa", "Colore camicia", "Couleur chemise", "上衣颜色", "Cor da camisa", "Kolor koszuli", "Цвет рубашки", "Cor da camisa", "Hemdfarbe" });

        public static string ColorTorso => T(new[] {
            "Undershirt color", "Color torso", "Colore maglietta", "Couleur t-shirt", "内衣颜色", "Cor da camisola", "Kolor podkoszulka", "Цвет майки", "Cor da camiseta", "Unterhemdfarbe" });

        public static string ColorPantalon => T(new[] {
            "Pants color", "Color pantalón", "Colore pantaloni", "Couleur pantalon", "裤子颜色", "Cor das calças", "Kolor spodni", "Цвет штанов", "Cor das calças", "Hosenfarbe" });

        public static string ColorZapatos => T(new[] {
            "Shoe color", "Color zapatos", "Colore scarpe", "Couleur chaussures", "鞋子颜色", "Cor dos sapatos", "Kolor butów", "Цвет обуви", "Cor dos sapatos", "Schuhfarbe" });
    }
}