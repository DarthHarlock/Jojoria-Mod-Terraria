using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Localization;

namespace Jojo.Content.Eventos.AvisoDiscrordControles
{
    public class AvisoDiscordControlesPlayer : ModPlayer
    {
        public bool haVistoMensaje = false;

        public override void SaveData(TagCompound tag)
        {
            tag["haVistoMensaje"] = haVistoMensaje;
        }

        public override void LoadData(TagCompound tag)
        {
            haVistoMensaje = tag.GetBool("haVistoMensaje");
        }

        public override void OnEnterWorld()
        {
            if (Player.whoAmI == Main.myPlayer && !haVistoMensaje)
            {
                string mensaje1;
                string mensaje2;

                switch (Language.ActiveCulture.Name)
                {
                    case "es-ES":
                        mensaje1 = "[c/FF0000:Importante:] [c/FFFF00:Mapea las teclas de las habilidades stand, en el menú de controles.]";
                        mensaje2 = "[c/FFFF00:No olvides unirte al servidor de] [c/5865F2:Discord] [c/FFFF00:para participar en la creación del mod o hacer preguntas.]";
                        break;

                    case "de-DE":
                        mensaje1 = "[c/FF0000:Wichtig:] [c/FFFF00:Weise die Tasten für deine Stand-Fähigkeiten im Steuerungsmenü zu.]";
                        mensaje2 = "[c/FFFF00:Vergiss nicht, unserem] [c/5865F2:Discord] [c/FFFF00:-Server beizutreten, um an der Mod-Entwicklung teilzunehmen oder Fragen zu stellen.]";
                        break;

                    case "it-IT":
                        mensaje1 = "[c/FF0000:Importante:] [c/FFFF00:Mappa i tasti delle abilità Stand nel menu dei controlli.]";
                        mensaje2 = "[c/FFFF00:Non dimenticare di unirti al nostro server] [c/5865F2:Discord] [c/FFFF00:per partecipare alla creazione della mod o fare domande.]";
                        break;

                    case "fr-FR":
                        mensaje1 = "[c/FF0000:Important :] [c/FFFF00:Associez les touches de vos compétences Stand dans le menu des contrôles.]";
                        mensaje2 = "[c/FFFF00:N'oubliez pas de rejoindre notre serveur] [c/5865F2:Discord] [c/FFFF00:pour participer à la création du mod ou poser des questions.]";
                        break;

                    case "ru-RU":
                        mensaje1 = "[c/FF0000:Важно:] [c/FFFF00:Назначьте клавиши способностей Stand в меню управления.]";
                        mensaje2 = "[c/FFFF00:Не забудьте присоединиться к нашему серверу] [c/5865F2:Discord] [c/FFFF00:, чтобы участвовать в создании мода или задавать вопросы.]";
                        break;

                    case "zh-Hans":
                        mensaje1 = "[c/FF0000:重要提示:] [c/FFFF00:请在控制菜单中绑定你的替身能力按键。]";
                        mensaje2 = "[c/FFFF00:别忘了加入我们的] [c/5865F2:Discord] [c/FFFF00:服务器，参与模组制作或提出问题。]";
                        break;

                    case "pt-BR":
                        mensaje1 = "[c/FF0000:Importante:] [c/FFFF00:Mapeie as teclas das habilidades do Stand no menu de controles.]";
                        mensaje2 = "[c/FFFF00:Não se esqueça de entrar no nosso servidor do] [c/5865F2:Discord] [c/FFFF00:para participar da criação do mod ou fazer perguntas.]";
                        break;

                    case "pl-PL":
                        mensaje1 = "[c/FF0000:Ważne:] [c/FFFF00:Przypisz klawisze umiejętności Stand w menu sterowania.]";
                        mensaje2 = "[c/FFFF00:Nie zapomnij dołączyć do naszego serwera] [c/5865F2:Discord] [c/FFFF00:, aby uczestniczyć w tworzeniu moda lub zadawać pytania.]";
                        break;

                    case "en-US":
                    default:
                        mensaje1 = "[c/FF0000:Important:] [c/FFFF00:Map your Stand ability keys in the controls menu.]";
                        mensaje2 = "[c/FFFF00:Don't forget to join our] [c/5865F2:Discord] [c/FFFF00:server to participate in the mod's creation or ask questions.]";
                        break;
                }

                Main.NewText(mensaje1);
                Main.NewText(mensaje2);

                haVistoMensaje = true;
            }
        }
    }
}