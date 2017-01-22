namespace LyricsDotNet {
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using IronPython.Hosting;
    using SpotifyAPI.Local;
    using SpotifyAPI.Local.Models;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private readonly SpotifyLocalAPI spotify;

        public MainWindow() {
            InitializeComponent();

            spotify = new SpotifyLocalAPI();
            spotify.Connect();
            spotify.ListenForEvents = true;

            spotify.OnTrackChange += TrackChanged;
            spotify.OnTrackTimeChange += TrackTimeChanged;

            var status = spotify.GetStatus();
            SetTrackInfo(status.Track);
        }

        private void TrackTimeChanged(object sender, TrackTimeChangeEventArgs e) {
            Dispatcher.Invoke(
                  () => {
                      SongProgress.Value = e.TrackTime;
                  });
        }

        public void TrackChanged(object sender, TrackChangeEventArgs e) {
            SetTrackInfo(e.NewTrack);
        }

        private void SetTrackInfo(Track track) {
            Dispatcher.Invoke(
                              () => {
                                  TrackInfo.Content = $"{track.TrackResource.Name} - {track.ArtistResource.Name}";
                                  SongProgress.Maximum = track.Length;
                                  SetLyrics(track);
                              });
        }

        private void SetLyrics(Track track) {
            string UrlFriendlyString(string s) => Regex.Replace(s.ToLower(), "[^A-Za-z0-9]+", "");

            var friendlyTrack = UrlFriendlyString(track.TrackResource.Name);
            var friendlyArtist = UrlFriendlyString(track.ArtistResource.Name);

            using (var client = new WebClient()) {
                Debug.WriteLine($"http://azlyrics.com/lyrics/{friendlyArtist}/{friendlyTrack}.html");
                var html =
                    client.DownloadString(new Uri($"http://azlyrics.com/lyrics/{friendlyArtist}/{friendlyTrack}.html"));
                var split = html.Split(
                                       new[] {
                                           "<!-- Usage of azlyrics.com content by any third-party lyrics provider is prohibited by our licensing agreement. Sorry about that. -->"
                                       },
                                       StringSplitOptions.None);
                var split_html = split[1];

                split = split_html.Split(
                                         new[] {
                                             "</div>"
                                         },
                                         StringSplitOptions.None);
                var lyrics = split[0];

                lyrics = Regex.Replace(lyrics, "(<.*?>)", "");
                lyrics = Regex.Replace(lyrics, "&quot;|&#34;|&#x22;|&ldquo;|&#147;|&#x93;|&rdquo;|&#148;|&#x94", "\"");
                lyrics = Regex.Replace(lyrics, "&amp;|&#38;|&#x26;", "&");
                lyrics = Regex.Replace(lyrics, "&lsquo;|&#145;|&#x91;|&#rsquo;|&#146;|&#x92;|&#39;|&#x27;", "'");
                lyrics = Regex.Replace(lyrics, "&#40;|&#x28;", "(");
                lyrics = Regex.Replace(lyrics, "&#41;|&#x29;", ")");
                lyrics = Regex.Replace(lyrics, "&#33;|&#x21;", "!");
                lyrics = Regex.Replace(lyrics, "&#42;|&#x2a;", "*");
                lyrics = Regex.Replace(lyrics, "&#44;|&#x2c;", ",");
                lyrics = Regex.Replace(lyrics, "&ndash;|&#150;|&#x96;|&mdash;|&#151;|&#x97;|&#45;|&#x2d;", "-");
                lyrics = Regex.Replace(lyrics, "&#46;|&#x2e;", ".");
                lyrics = Regex.Replace(lyrics, "&#59;|&#x3b;", ";");
                lyrics = Regex.Replace(lyrics, "&#63;|&#x3f;", "?");
                lyrics = Regex.Replace(lyrics, "&#64;|&#x40;", "@");
                lyrics = Regex.Replace(lyrics, "&#35;|&#x23;", "#");
                lyrics = Regex.Replace(lyrics, "&#91;|&#x5b;", "[");
                lyrics = Regex.Replace(lyrics, "&trade;|&#153;|&#x99;", "TM");

                Debug.WriteLine(lyrics);
                Lyrics.Text = lyrics;
            }
        }
    }
}
