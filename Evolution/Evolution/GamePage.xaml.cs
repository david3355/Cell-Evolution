﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Text;
//using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using System.Windows.Media.Imaging;
using System.Threading;
using System.IO;
using System.IO.IsolatedStorage;

namespace Evolution
{
    public partial class GamePage : PhoneApplicationPage
    {
        public static Uri GetUri()
        {
            return Uri;
        }

        #region Fields

        static Uri Uri = new Uri("/GamePage.xaml", UriKind.Relative);

        ContentManager contentManager;  // A tartalmak betöltéséhez szükséges
        GameTimer timer;
        SpriteBatch spriteBatch; // A rajzolásért/megjelenítésért felelős grafikai objektum

        Player player;
        Texture2D tx_player;
        Texture2D tx_enemy_smaller;
        Texture2D tx_enemy_bigger;
        Texture2D tx_antimatter;
        Texture2D tx_sdinf;
        Texture2D tx_iminf;
        Texture2D tx_bG;
        Texture2D tx_rage;

        Song s_music;
        SoundEffect se_collosion, se_move, se_infection, se_extinct, se_rage;

        int n_enemy, n_antim, n_inf;
        float speed;
        public static float musicVolume = 1f, effectsVolume = 0.4f;
        public static string playerName;

        List<Objects> objects;
        int level, score;

        Random rnd = new Random();
        SpriteFont sf, sfmgs;
        GameTimer gt_sdi, gt_imi, gtse, gt_game, gt_rageOn; // Az infection objektumok időzítői


        int t_imi, t_sdi; // time of inverse moving infection / time of size decreasing infection
        int t_se, t_game, t_rageOn;

        bool touching, twoTouches, levelEnd, canTwoTouch, infected, rageObject, rageOn;
        int terminated;
        #endregion Fields

        void NewLevel()
        {
            level += 1;
            speed += 0.1f;
            n_enemy += 1;
            n_antim += 1;
            n_inf += 1;
            t_game = 0;
            Initialize();
        }

        void Initialize()
        {
            levelEnd = false;
            canTwoTouch = false;
            twoTouches = false;
            terminated = 0;

            effectsVolume = (float)Settings.slideValEffects;

            //player:
            Vector2 velocity = Vector2.Zero;
            Vector2 center = new Vector2(400, 240);
            player = new Player(this, tx_player, center, velocity, 10);
            //all other objects:
            objects = new List<Objects>();
            AddObjects(new Enemy(), tx_enemy_smaller, n_enemy * 2, 10, GetRanVelocity(speed));
            AddObjects(new Enemy(), tx_enemy_bigger, n_enemy, -30, GetRanVelocity(speed));
            AddObjects(new AntiMatter(), tx_antimatter, n_antim, 30, GetRanVelocity(speed));
            AddObjects(new SizeDecrease(), tx_sdinf, n_inf, 0, Vector2.Zero);
            AddObjects(new InverseMoving(), tx_iminf, n_inf, 0, Vector2.Zero);

            gt_sdi.Stop(); t_sdi = 0; // az új pálya kezdésekor nem lehet infection
        }
        // E metódus segítségébel az objektumokat szignatúra alapján, univerzálisan tudjuk hozzáadni a listához
        void AddObjects(Objects obj, Texture2D texture, int number, int maxRad, Vector2 velocity)
        {
            float ran, X, Y, dist = 100;
            for (int i = 0; i < number; i++)
            {
                X = rnd.Next(50, 750);
                Y = rnd.Next(50, 430);
                // ezeken majd még javítani kell, hogy univerzális legyen:
                //X = rnd.Next(maxRad * 2, (int)this.ActualHeight - maxRad * 2);
                //Y = rnd.Next(maxRad * 2, (int)this.ActualWidth - maxRad * 2);
                if (X < player.Origo.X + dist && X > player.Origo.X - dist && Y < player.Origo.Y + dist && Y > player.Origo.Y - dist) // távolságot kell, hogy hagyjunk a player és az új objektumok között
                {
                    i--;
                    continue;
                }
                if (maxRad == 0) ran = 13; // az infection-ök mérete rögzített
                else if (maxRad < 0) ran = rnd.Next((int)player.R, Math.Abs(maxRad)); // ha nagyobb ellenséget adunk hozzá, tudnunk kell róla, így csak a player sugara és maxRad között generálhatunk számokat
                else ran = rnd.Next(3, maxRad);
                if (obj is Enemy) obj = new Enemy();
                else if (obj is AntiMatter) obj = new AntiMatter();
                else if (obj is SizeDecrease) obj = new SizeDecrease();
                else if (obj is InverseMoving) obj = new InverseMoving();
                obj.SetAttributes(this, texture, new Vector2(X, Y), velocity, ran);
                objects.Add(obj);
            }
        }

        Vector2 GetRanVelocity(float speed)
        {
            return new Vector2(rnd.Next(2) < 1 ? speed : -speed, rnd.Next(2) < 1 ? speed : -speed);
        }

        public GamePage()
        {
            InitializeComponent();

            // Get the content manager from the application
            contentManager = (Application.Current as App).Content;

            // Create a timer for this page
            timer = new GameTimer();
            timer.UpdateInterval = TimeSpan.FromTicks(333333);
            timer.Update += OnUpdate;
            timer.Draw += OnDraw;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Set the sharing mode of the graphics device to turn on XNA rendering
            SharedGraphicsDeviceManager.Current.GraphicsDevice.SetSharingMode(true);

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(SharedGraphicsDeviceManager.Current.GraphicsDevice);
            //contentManager.RootDirectory = "Content";
            // TODO: use this.content to load your game content here
            tx_bG = contentManager.Load<Texture2D>("bg");
            tx_player = contentManager.Load<Texture2D>("player");
            tx_enemy_smaller = contentManager.Load<Texture2D>("enemy_smaller");
            tx_enemy_bigger = contentManager.Load<Texture2D>("enemy_bigger");
            tx_antimatter = contentManager.Load<Texture2D>("antimatter");
            tx_sdinf = contentManager.Load<Texture2D>("sizedecinf");
            tx_iminf = contentManager.Load<Texture2D>("inverseinf");
            tx_rage = contentManager.Load<Texture2D>("rage");
            sf = contentManager.Load<SpriteFont>("myFont");
            sfmgs = contentManager.Load<SpriteFont>("SFmgs");
            s_music = contentManager.Load<Song>("BGMusic");
            se_collosion = contentManager.Load<SoundEffect>("collosion");
            se_extinct = contentManager.Load<SoundEffect>("death");
            se_move = contentManager.Load<SoundEffect>("move");
            se_infection = contentManager.Load<SoundEffect>("infection");
            se_rage = contentManager.Load<SoundEffect>("flame");
            n_enemy = 10;
            n_antim = 10;
            n_inf = 3;
            speed = 0.1f;
            level = 0;
            score = 0;
            gt_sdi = new GameTimer();
            gt_sdi.UpdateInterval = new TimeSpan(0, 0, 1);
            gt_sdi.Update += gt_imi_Update;
            gt_imi = new GameTimer();
            gt_imi.UpdateInterval = new TimeSpan(0, 0, 1);
            gt_imi.Update += gt_sdi_Update;
            gtse = new GameTimer();
            gtse.UpdateInterval = TimeSpan.FromMilliseconds(1);
            gtse.Update += gtse_Update;
            gt_game = new GameTimer();
            gt_game.UpdateInterval = TimeSpan.FromSeconds(1);
            gt_game.Update += gt_game_Update;
            gt_rageOn = new GameTimer();
            gt_rageOn.UpdateInterval = TimeSpan.FromSeconds(1);
            gt_rageOn.Update += gt_rageOn_Update;
            t_se = 0;
            t_game = 0;

            gt_sdi.Start();
            gt_game.Start();

            SharedGraphicsDeviceManager.Current.GraphicsDevice.Clear(Color.White);
            NewLevel();

            if (Settings.stopMusic)
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume = musicVolume;
                MediaPlayer.Play(s_music);
            }
            else if (MediaPlayer.State != MediaState.Playing)
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume = musicVolume;
                MediaPlayer.Play(s_music);
            }

            timer.Start();

            base.OnNavigatedTo(e);
        }

        void gt_imi_Update(object sender, GameTimerEventArgs e)
        {
            t_imi -= 1;
        }

        void gt_sdi_Update(object sender, GameTimerEventArgs e)
        {
            t_sdi -= 1;
        }

        void gtse_Update(object sender, GameTimerEventArgs e)
        {
            t_se += 1;
        }

        void gt_game_Update(object sender, GameTimerEventArgs e)
        {
            t_game += 1;
        }

        void gt_rageOn_Update(object sender, GameTimerEventArgs e)
        {
            t_rageOn += 1;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Stop the timer
            timer.Stop();

            // Set the sharing mode of the graphics device to turn off XNA rendering
            SharedGraphicsDeviceManager.Current.GraphicsDevice.SetSharingMode(false);
            //NavigationService.Navigate(MainPage.GetUri());
            //NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            //base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Allows the page to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        private void OnUpdate(object sender, GameTimerEventArgs e)
        {
            // TODO: Add your update logic here
            HandleTouches();
            UpdateObjects();
            IsLevelEnd();
            if (player.velocity.X > 0) player.velocity.X -= 0.1f;
            if (player.velocity.X < 0) player.velocity.X += 0.1f;
            if (player.velocity.Y > 0) player.velocity.Y -= 0.1f;
            if (player.velocity.Y < 0) player.velocity.Y += 0.1f;
            if (Math.Abs(player.velocity.Y) < 0.11) player.velocity.Y = 0;
            if (Math.Abs(player.velocity.X) < 0.11) player.velocity.X = 0;
            if (terminated == 1)
            {
                se_extinct.Play(effectsVolume, 0, 0);
                objects.Clear();
                terminated = -1;
            }
            if (levelEnd && !canTwoTouch) canTwoTouch = true;
            if (levelEnd && HighScores.hLevel < level) HighScores.hLevel = level;

            if (levelEnd && twoTouches) NewLevel();
            if (t_game > 0 && t_game % 60 == 0 && !rageObject)
            {
                rageObject = true;
                AddObjects(new Rage(), tx_rage, 1, 0, Vector2.Zero);
            }
            if (t_game > 0 && t_game % 66 == 0 && rageObject)
            {
                rageObject = false;
                foreach (Objects obj in objects) if (obj is Rage) { objects.Remove(obj); break; }
            }
        }

        /// <summary>
        /// Allows the page to draw itself.
        /// </summary>
        private void OnDraw(object sender, GameTimerEventArgs e)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(tx_bG, Vector2.Zero, Color.White);

            if (!twoTouches && terminated == 0) // Amíg nincs double tap és a játékos él, addig kirajzoljuk az objektumokat és az adatokat.
            {
                if (player.R > 0) player.Draw(spriteBatch);
                foreach (Objects en in objects) en.Draw(spriteBatch);
                spriteBatch.DrawString(sf, "Level " + level, new Vector2(10, 10), Color.White);
                spriteBatch.DrawString(sf, "Score: " + score, new Vector2(10, 30), Color.White);
            }
            else if (!levelEnd && player.R <= 0)
            {
                spriteBatch.DrawString(sfmgs, "You have been extinct", new Vector2(250, (int)this.ActualHeight / 2), Color.Red);
            }
            if (levelEnd)
            {
                spriteBatch.DrawString(sfmgs, "Level completed, double-tap to new level", new Vector2(150, 10), Color.Green);
            }
            spriteBatch.End();
        }

        void HandleTouches()
        {
            TouchCollection touches = TouchPanel.GetState();
            if (!touching && touches.Count > 0 && terminated == 0)
            {
                touching = true;
                se_move.Play(effectsVolume, 0, 0);
                SetNewVelocity(touches);
                if (canTwoTouch && touches.Count == 2) twoTouches = true;
                else twoTouches = false;
            }
            else if (touches.Count == 0)
            {
                touching = false;
                twoTouches = false;
            }
        }

        void SetNewVelocity(TouchCollection touch)
        {
            // itt állítjuk be, hogy kattintás (tap) hatására milyen irányba és sebességgel mozduljon el a player
            float speed = 1.5f;
            Vector2 K = player.Origo;
            Vector2 T = touch[0].Position;
            Vector2 V = new Vector2();
            V.X = K.X - T.X;
            V.Y = K.Y - T.Y;
            if (Math.Abs(V.X) > Math.Abs(V.Y))
            {
                V.Y /= Math.Abs(V.X);
                V.X /= Math.Abs(V.X);
            }
            else
            {
                V.X /= Math.Abs(V.Y);
                V.Y /= Math.Abs(V.Y);
            }
            V.X *= speed;
            V.Y *= speed;
            player.velocity.X += V.X;
            player.velocity.Y += V.Y;
            // inverse moving infection esetén:
            if (!infected && t_imi > 0)
            {
                player.velocity.X *= -1;
                player.velocity.Y *= -1;
                infected = true;
            }

            if (player.velocity.X > 10) player.velocity.X = 10;
            if (player.velocity.Y > 10) player.velocity.Y = 10;
        }

        void UpdateObjects()
        {
            if (t_imi <= 0)
            {
                gt_sdi.Stop();
                infected = false;
            }
            if (t_sdi <= 0) gt_imi.Stop();
            if (t_sdi > 0) player.R -= 0.03f;  // ha épp elkapott egy size decreasing infectiont, amíg tart a fertőzés, csökken a mérete
            if (t_rageOn >= 5)
            {
                t_rageOn = 0;
                gt_rageOn.Stop();
                rageOn = false;
                player.ChangeTexture(tx_player);
            }

            foreach (Objects e in objects)
            {
                if (e is Enemy && e.R < player.R) e.ChangeTexture(tx_enemy_smaller);
                else if (e is Enemy && e.R >= player.R) e.ChangeTexture(tx_enemy_bigger);
            }
            player.Update();
            foreach (Objects e in objects)
                e.Update();
            CollosionObjects();
            if (player.R <= 0 && terminated == 0 && !levelEnd) terminated = 1;
        }

        void CollosionTest(Objects b1, Objects b2)
        {
            float x, k;
            double d; // Distance
            d = Math.Sqrt(Math.Pow(b1.Origo.X - b2.Origo.X, 2) + Math.Pow(b1.Origo.Y - b2.Origo.Y, 2));
            k = (float)((b1.R + b2.R) - d) / 2;
            x = 1f;
            if (b1 is Player && b2 is Infection && d <= b1.R + b2.R)
            {
                if (b2 is Rage) se_rage.Play(effectsVolume, 0, 0);
                else se_infection.Play(effectsVolume, 0, 0);
                if (b2 is InverseMoving)
                {
                    t_imi = 5;
                    gt_sdi.Start();
                }
                else if (b2 is SizeDecrease)
                {
                    t_sdi = 5;
                    gt_imi.Start();
                }
                else if (b2 is Rage) // MEGÍRNI!!!!!!!
                {
                    player.ChangeTexture(tx_rage);
                    rageOn = true;
                    gt_rageOn.Start();
                }
                objects.Remove(b2);
            }
            else if (b2 is AntiMatter && d < b1.R + b2.R)
            {
                b1.R -= k;
                b2.R -= k;
                if (b1 is Player && terminated == 0) CollosionSound();

            }
            else if (d <= b1.R + b2.R)
            {
                if (b1 is Player && rageOn)
                {
                    b1.R += x * 0.2f;
                    b2.R -= x * 0.5f;
                }
                else if (b1.R > b2.R)
                {
                    b1.R += x * 0.2f;
                    b2.R -= x;
                }
                else if (b1.R < b2.R)
                {
                    b2.R += x * 0.2f;
                    b1.R -= x;
                }
                else
                {
                    b1.velocity *= -1;
                    b2.velocity *= -1;
                }
                if (b1 is Player) CollosionSound();
            }
            if (!(b1 is Player) && b1.R <= 0) objects.Remove(b1);
            if (b2.R <= 0) objects.Remove(b2);
            if (b1 is Player && !(b2 is AntiMatter) && b2.R <= 0)
            {
                score += 10;
                if (HighScores.hScore < score) HighScores.hScore = score;
            }
        }

        void CollosionSound()
        {
            if (t_se == 0)
            {
                se_collosion.Play(effectsVolume, 0, 0);
                gtse.Start();
            }
            else if (t_se >= se_collosion.Duration.TotalMilliseconds)
            {
                t_se = 0;
                gtse.Stop();
            }
        }

        void UnStuck(Objects obj) // Itt azt vizsgáljuk, hogy ha a megváltozott objektum kilóg, akkor annyival mozgatjuk el a képernyő felé
        {

        }

        void CollosionObjects()
        {
            for (int i = 0; i < objects.Count; i++)
            {
                for (int j = i + 1; j < objects.Count; j++)
                {
                    if (objects[i] is Infection || objects[j] is Infection) continue;  // az infection-ök csak a playerre vonatkoznak
                    CollosionTest(objects[i], objects[j]);
                }
            }
            for (int i = 0; i < objects.Count; i++)
            {
                CollosionTest(player, objects[i]);
            }
        }

        void IsLevelEnd()
        {
            // ha van olyan ellenség, mely nagyobb a playernél, még nincs vége
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] is Enemy && objects[i].R > player.R) return;
            }
            if (terminated == 0) levelEnd = true;
        }


    }


}