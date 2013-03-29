using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MinorhythmListener.Models
{
    /// <summary>
    /// radio minorhythm 内の情報を表します。
    /// </summary>
    public class Minorhythm
    {
        private static readonly Uri _Address = new Uri("http://lantis-net.com/minorhythm/");
        private static readonly Uri _BackNumber = new Uri(_Address, "backnumber.html");
        private static readonly Uri _Mailform = new Uri(_Address, "mailform.html");

        private Dictionary<string, string> _Corners;
        private List<Content> _Contents;
        private ThemeSong _Opening, _Ending;

        #region Property

        /// <summary>
        /// radio minorhythm の公式サイトのアドレスを取得します。
        /// </summary>
        public static Uri Address
        {
            get { return _Address; }
        }

        /// <summary>
        /// radio minorhythm のバックナンバーページのアドレスを取得します。
        /// </summary>
        public static Uri BackNumber
        {
            get { return _BackNumber; }
        }

        /// <summary>
        /// radio minorhythm のメールフォームのアドレスを取得します。
        /// </summary>
        public static Uri Mailform
        {
            get { return _Mailform; }
        }

        /// <summary>
        /// コーナーのタイトルと説明の組を取得します。
        /// </summary>
        public IReadOnlyDictionary<string, string> Corners
        {
            get { return _Corners; }
        }

        /// <summary>
        /// ラジオの情報の一覧を取得します。
        /// </summary>
        public IReadOnlyCollection<Content> Contents
        {
            get { return _Contents; }
        }

        /// <summary>
        /// オープニングテーマの情報を取得します。
        /// </summary>
        public ThemeSong Opening
        {
            get { return _Opening; }
        }

        /// <summary>
        /// エンディングテーマの情報を取得します。
        /// </summary>
        public ThemeSong Ending
        {
            get { return _Ending; }
        }

        #endregion

        private Minorhythm()
        {
            _Corners = new Dictionary<string, string>();
            _Contents = new List<Content>();
        }

        private async Task Initialize()
        {
            XDocument xml;

            var client = new WebClient();
            client.Encoding = Encoding.UTF8;
            var html = await client.DownloadStringTaskAsync(Address);
            html = html.Replace("&nbsp;", " ").Replace("&copy;", "(c)");
            html = html.Insert(html.IndexOf("</body>"), "</div>");
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, html, Encoding.UTF8);
            using (var reader = new StreamReader(temp))
            {
                xml = XDocument.Load(reader);
            }
            
            var ns = xml.Root.Name.Namespace;
            var div = ns + "div";
            var span = ns + "span";
            var h3 = ns + "h3";
            var h4 = ns + "h4";
            var p = ns + "p";
            var a = ns + "a";

            // 最新回の情報
            var latest = xml.Descendants(div)
                            .Where(e => e.Attribute("id") != null && e.Attribute("id").Value == "radio")
                            .Descendants()
                            .Where(e => e.Attribute("class") != null && e.Attribute("class").Value == "wbody")
                            .First();

            _Contents.Add(new Content(latest.Element(h3).Value,
                                      latest.Element(h3).Element(span).Value,
                                      latest.Element(p).Value));

            // コーナーの情報
            var corner = xml.Descendants(div)
                            .Where(e => e.Attribute("id") != null && e.Attribute("id").Value == "corner")
                            .Descendants();

            _Corners = corner.Where(e => e.Name == h3)
                             .Select(e => e.Value)
                             .Zip(corner.Where(e => e.Name == p).Select(e => e.Value),
                                  (e1, e2) => new KeyValuePair<string, string>(e1, e2))
                             .ToDictionary(x => x.Key, x => x.Value);

            // テーマソングの情報
            var theme = xml.Descendants(div)
                           .Where(e => e.Attribute("id") != null && e.Attribute("id").Value == "theme")
                           .Descendants()
                           .Where(e => e.Name == a);

            _Opening = new ThemeSong(theme.First(), ns);
            _Ending = new ThemeSong(theme.Skip(1).First(), ns);

            // バックナンバーの情報
            html = await client.DownloadStringTaskAsync(BackNumber);
            html = html.Replace("&nbsp;", " ").Replace("&copy;", "(c)").Replace("</span></p>", "</span>");
            html = html.Insert(html.IndexOf("</body>"), "</div>");
            client.Dispose();
            File.WriteAllText(temp, html, Encoding.UTF8);
            using (var reader = new StreamReader(temp))
            {
                xml = XDocument.Load(reader);
            }

            var backNumber = xml.Descendants(div)
                                .Where(e => e.Attribute("class") != null && e.Attribute("class").Value == "wbody");

            _Contents.AddRange(backNumber.Descendants(h3)
                                         .Zip(backNumber.Descendants(p).Where(e => !e.HasAttributes),
                                              (e1, e2) => new KeyValuePair<XElement, XElement>(e1, e2))
                                         .Select(x => new Content(x.Key.Value, x.Key.Element(span).Value, x.Value.Value)));
        }

        /// <summary>
        /// radio minorhythm の情報を読み込み、新しいインスタンスを初期化して返します。
        /// </summary>
        public static async Task<Minorhythm> Load()
        {
            if (!NetworkInterface.GetIsNetworkAvailable()) return null;

            var radio = new Minorhythm();
            try
            {
                await radio.Initialize();
            }
            catch(Exception ex)
            {
                Logger.WriteError(ex);
                throw new TypeInitializationException(radio.GetType().FullName, ex);
            }

            return radio;
        }
    }

    /// <summary>
    /// ラジオの各回の情報を表します。
    /// </summary>
    public struct Content
    {
        /// <summary>
        /// 回数を取得します。
        /// </summary>
        public int Number { get; private set; }
        /// <summary>
        /// 配信日を取得します。
        /// </summary>
        public DateTime Date { get; private set; }
        /// <summary>
        /// 配信日と曜日を表す文字列を取得します。
        /// </summary>
        public string DateString { get; private set; }
        /// <summary>
        /// 配信されている URI を取得します。
        /// </summary>
        public Uri Address { get; private set; }
        /// <summary>
        /// 説明を取得します。
        /// </summary>
        public string Description { get; private set; }

        private static char[] separator = { '.', ' ' };

        /// <summary>
        /// 公式サイトの表記の文字列を基に新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="number">回数を表す文字列。</param>
        /// <param name="dateString">日付を表す文字列。</param>
        /// <param name="description">内容のテキスト。</param>
        public Content(string number, string dateString, string description)
        {
            this = new Content();
            this.Number = int.Parse(number.Substring(1, 3));
            this.DateString = dateString;
            var dateSplit = dateString.Split(separator);
            this.Date = new DateTime(int.Parse(dateSplit[0]), int.Parse(dateSplit[1]), int.Parse(dateSplit[2]));
            this.Address = new Uri(string.Join("", Minorhythm.Address, dateSplit[0].Substring(2), dateSplit[1], dateSplit[2], "h.asx"));
            this.Description = description;
        }

        /// <summary>
        /// 先頭に # を付加した回数を表す文字列を取得します。
        /// </summary>
        public override string ToString()
        {
            return "#" + Number.ToString();
        }

        public override bool Equals(object obj)
        {
            return this.Number == ((Content)obj).Number;
        }

        public override int GetHashCode()
        {
            return this.Number;
        }

        public static bool operator ==(Content c1, Content c2)
        {
            return c1.Number == c2.Number;
        }

        public static bool operator !=(Content c1, Content c2)
        {
            return c1.Number != c2.Number;
        }
    }

    /// <summary>
    /// テーマソングの情報を表します。
    /// </summary>
    public struct ThemeSong
    {
        /// <summary>
        /// タイトルを取得します。
        /// </summary>
        public string Title { get; private set; }
        /// <summary>
        /// 説明を取得します。
        /// </summary>
        public string Description { get; private set; }
        /// <summary>
        /// テーマソングが収録されているアルバムジャケット画像の URI を取得します。
        /// </summary>
        public Uri ImageUri { get; private set; }

        /// <summary>
        /// XML 要素から抽出したテーマソングの情報を使用して新しいインスタスを初期化します。
        /// </summary>
        /// <param name="element">テーマソングの情報を含む XML 要素</param>
        /// <param name="ns">抽出する XML 要素の XML 名前空間</param>
        public ThemeSong(XElement element, XNamespace ns)
        {
            this = new ThemeSong();
            try
            {
                var h4 = ns + "h4";
                var title = element.Element(h4);
                this.Title = title.Value;
                this.Description = title.NextNode.ToString();
                var img = ns + "img";
                this.ImageUri = new Uri(Minorhythm.Address, element.Element(img).Attribute("src").Value);
            }
            catch
            {
                throw new ArgumentException("XML 要素からテーマソング情報を抽出できません。");
            }
        }

        /// <summary>
        /// 指定したタイトル、説明、画像のアドレスで新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="title">テーマソングのタイトル。</param>
        /// <param name="description">テーマソングの説明。</param>
        /// <param name="imageUri">テーマソングが収録されているアルバムジャケット画像の URI。</param>
        public ThemeSong(string title, string description, Uri imageUri)
        {
            this = new ThemeSong();
            this.Title = title;
            this.Description = description;
            this.ImageUri = imageUri;
        }
    }
}
