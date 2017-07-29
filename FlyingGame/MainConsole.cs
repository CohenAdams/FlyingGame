﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System;
using FlyingGame.Model.EnemyJets;
using FlyingGame.Model.MyJet;
using FlyingGame.Model.Shared;
using Brushes = System.Drawing.Brushes;
using Color = System.Drawing.Color;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using Pen = System.Drawing.Pen;

namespace FlyingGame
{
    public partial class MainConsole : Form
    {
        //Instantiate required objects
        private Random rand = new Random();
        
        //For game control
        private GameController _gc = new GameController();
        //For keyboard input
        private KeyboardInput _ki = new KeyboardInput();

        //For ground mountain background
        private GroundObject _go = new GroundObject();
        
        //For cloud background
        private SkyObjects _so = new SkyObjects();
        private List<CloudFactory> _cL = new List<CloudFactory>();              //Create list of clouds for multiple clouds generation
        
        //For my jet
        private MyJet _mj = new MyJet();
        private MyJetBomb _mjb = new MyJetBomb();
        private List<MyJetGunBullet> _mjg = new List<MyJetGunBullet>();         //Create list of bullets for multiple bullets generation
        private Pen _myJetBulletColor;
        
        //For power ups 
        private PowerUps _pu = new PowerUps();
        
        //For enemy jet
        private List<EnemyJet> _ej = new List<EnemyJet>();                      //Create list of enemy jets for multiple jets generation
        private List<EnemyJetGunBullet> _ejg = new List<EnemyJetGunBullet>();   //Create list of bullets for multiple bullets generation
        
        //For explosion animation
        private List<Explosion> _exp = new List<Explosion>();           

        //For boss
        private EnemyBoss _eb = new EnemyBoss();
        
        public MainConsole()
        {
            InitializeComponent();
            _mj.MovementState = 0;
            _ki.KeyDirectionVertical = 0;
            _gc.IsGameOver = true;
            LblStartEndBanner.Visible = true;
            LblStartEndBanner.Text = @"Hit Enter to start the game.";
            
            timer1.Interval = 1000 / _gc.GameSpeed;
            timer1.Tick += UpdateSky;
            timer1.Start();
        }

        private void StartGame()
        {
            _gc = new GameController();
            _ki = new KeyboardInput();

            _go = new GroundObject();
            _so = new SkyObjects();

            _mj = new MyJet();
            _mjb = new MyJetBomb();
            _pu = new PowerUps();
            _eb = new EnemyBoss();

            _myJetBulletColor = new Pen(Brushes.Blue);
            BackColor = Color.Empty;

            //Clear all lists when game restarts
            _ej.Clear();
            _ejg.Clear();
            _mjg.Clear();
            _exp.Clear();
            _cL.Clear();
        }
        
        private void UpdateSky(object sender, EventArgs e)
        {
            Graphics sky = PbConsole.CreateGraphics();

            StartRoutine();

            if (_gc.IsGameOver)
            {
                #region RestartGame
                if (_ki.RestartGame)
                {
                    LblInfo.Visible = false;
                    LblStartEndBanner.Visible = false;
                    LblBoss.Visible = false;
                    LblBossRem.Visible = false;
                    StartGame();
                    _ki.RestartGame = false;
                }
                #endregion
            }
            else
            {
                if (_mjb.BombXploded) goto BombExploded;                //no other action/drawing till the bomb explosion finished.

                sky.Clear(Color.LightSkyBlue);                          //clear all drawings from previous round (routine)
                
                #region Ground

                //Initiate
                if (rand.Next(1, 10) == 5 && !_go.MountainDrawInitiated)
                {
                    _go.MountainDrawInitiated = true;
                    _go.CountToMountainHgt = rand.Next(20, 50);     //Get a random number to control mountain Height
                    _go.CurrCountToMountainHgt = 0;
                    _go.Ydelta = rand.Next(1, 5);                   //Get a random number which will be applied in Y to increase mountain height
                    _go.Xdelta = 5;                                 //increase/decrease to speed up/down mountain slide speed

                    //Start from far right
                    _go.X1 = PbConsole.Width;
                    _go.X2 = PbConsole.Width;
                    _go.X3 = PbConsole.Width;

                    //Start from ground level
                    _go.Y1 = PbConsole.Height;
                    _go.Y2 = PbConsole.Height;
                    _go.Y3 = PbConsole.Height;

                    switch (rand.Next(1, 3))                                //select random colour from three choices as mountain colour
                    {
                        case 1:{_go.MountainColor = Brushes.Green;break;}
                        case 2:{_go.MountainColor = Brushes.Yellow;break;}
                        case 3:{_go.MountainColor = Brushes.SaddleBrown;break;}
                    }
                }

                if (!_go.MountainDrawInitiated) goto MyJetRegion;           //no mountain to draw, bypass this region

                //Process mountain points and draw
                
                _go.CurrCountToMountainHgt++;

                if (_go.CurrCountToMountainHgt >= _go.CountToMountainHgt)
                    _go.CurrCountToMountainHgt = _go.CountToMountainHgt;    //Do not allow current count to be greater than set count

                _go.X1 = _go.X1 - _go.Xdelta;                               //Constant Shift on left of left most X1 (mountain start point) 

                if (_go.CurrCountToMountainHgt < _go.CountToMountainHgt)
                    _go.Y2 = _go.Y2 - _go.Ydelta;                           //Reduce Y till Y reaches to desired mountain height (mountain starting to go up)

                _go.X2 = _go.CurrCountToMountainHgt == _go.CountToMountainHgt ? _go.X2 - _go.Xdelta : PbConsole.Width;  //if reached mountain height, shift X2 (mountain mid point) to left 
                _go.Y3 = _go.CurrCountToMountainHgt == _go.CountToMountainHgt ? _go.Y3 + _go.Ydelta : _go.Y2;           //if reached mountain height, increase Y3 by delta, else same value as Y2 (mountain starting to go down)
                _go.X3 = (_go.Y3 > PbConsole.Height ? _go.X3 - _go.Xdelta : PbConsole.Width);                           //X3 (mountain last point) shift to left when Y3 reached ground (mountain generation completed)

                
                if (_go.X3 < 0) //Whole mountain left the window, mark it for removal from list (during Cleanup)
                {
                    _go.MountainDrawInitiated = false;
                }

                if (_go.Y3 > PbConsole.Height)  //Stop the Y3 when reached the ground.
                    _go.Y3 = PbConsole.Height;

                //Create Polygon points to draw mountain
                var mPoints = new Point[5];
                mPoints[0] = new Point(_go.X1, _go.Y1);             //Start point, always at ground level
                mPoints[1] = new Point(_go.X2-5, _go.Y2);           //To give the mountain a flat top
                mPoints[2] = new Point(_go.X2 + 5, _go.Y2);         //To give the mountain a flat top
                mPoints[3] = new Point(_go.X3, _go.Y3);             
                mPoints[4] = new Point(_go.X3, PbConsole.Height);   //End of mountain - always at ground level

                sky.FillPolygon(_go.MountainColor, mPoints);

                #endregion

                #region Cloud

                if(_gc.SkyobjectOff) goto MyJetRegion;
                //only initiate when existing cloud is lesser than 4 and no incompleted cloud (to avoid major overlap between clouds)
                if (rand.Next(1, 150) == 50 && _cL.Count < 4 && (_cL.Select(x=>x).Where(x=>!x.IsGenerated).Count()==0))
                {
                    _so = new SkyObjects();
                    _so.Width = (byte) rand.Next(50, 200);                          //Get random width for cloud
                    _so.Height = (byte) rand.Next(30, 100);                         //Get random height for cloud
                    _so.RefX = PbConsole.Width;                                     //Start from far right
                    _so.RefY = rand.Next(5, PbConsole.Height/2 - _so.Height);       //Start somewhere between top half screen

                    switch (rand.Next(1,8))                                         //Get colour randomly from four choices
                    {
                        case 1: case 4:{_so.FillColor = Brushes.PowderBlue;break;}
                        case 2: case 5:{_so.FillColor = Brushes.DarkGray;break;}
                        case 3: case 6:{_so.FillColor = Brushes.LightGray;break;}
                        default:_so.FillColor = Brushes.White;break;
                    }

                    _so.DeltaX = (byte) rand.Next(1, 3);                            //Assign random speed for cloud (1 being slowest/ 3 being highest)

                    if (rand.Next(1, 10) == 5) _so.HasBorder = true;                //Randomly decide whether to apply border

                    //Add the cloud object in list.
                    _cL.Add(new CloudFactory{CloudInitiated = true,RefX = _so.RefX, FillColor = _so.FillColor,DeltaX = _so.DeltaX, Width = _so.Width, ShapePoints = _so.CloudPoints(),IsGenerated = false, HasBorder = _so.HasBorder});
                }
                
                if(_cL.Count==0) goto MyJetRegion;  //No cloud to process, leave this region

                //Process cloud movement and draw
                foreach (var cloud in _cL)
                {
                    //cloud.DeltaX = 1;                                                                   //Increase/decrease the delta to speed up/down cloud slide
                    cloud.RefX = cloud.RefX - cloud.DeltaX;

                    if (cloud.RefX + cloud.Width < 0) cloud.CloudInitiated = false;                      //If the whole cloud left windown, mark it for removal
                    if (cloud.RefX + (cloud.Width * _gc.CloudGenerationRate/100) <= PbConsole.Width) cloud.IsGenerated = true;   //If cloud shifted on left enough to be half drawn, mark it (to allow next cloud generation)

                    if (cloud.CloudInitiated)                   
                    {
                        sky.FillClosedCurve(cloud.FillColor,cloud.ProcessedShapePoints());                                  //Draw cloud

                        if(cloud.HasBorder) sky.DrawClosedCurve(new Pen(Color.DimGray), cloud.ProcessedShapePoints());      //Draw border

                        cloud.ShapePoints = cloud.ProcessedShapePoints();                                                   //Update shapepoints with current shapepoints
                    }
                }
                
                #endregion

                #region MyJet

            MyJetRegion:
                
                //jet gun fire - only when existing bullet is lesser than set limit (per active gun)
                if (_ki.KeyFire && _mjg.Count < _mj.CurrentBulletLimit * _mj.CurrentActiveGun)
                {
                    if (_gc.EnableSound)    //gun fire sound
                    {
                        var spJetFire = new MediaPlayer();
                        spJetFire.Open(new Uri(@"G:\VSprojects\FlyingGame\FlyingGame\Sounds\fire.wav"));
                        spJetFire.Play();
                    }

                    switch (_mj.CurrentActiveGun)   //Add bullets in bullet list based on active gun number
                    {
                        case 1:
                        {
                            _mjg.Add(new MyJetGunBullet {GunFireStart = true, X = _mj.X2, Y = _mj.Y2 - 10, Size = 3});
                            break;
                        }
                        case 2:
                        {
                            _mjg.Add(new MyJetGunBullet { GunFireStart = true, X = _mj.X2, Y = _mj.Y2 - 10, Size = 3});
                            _mjg.Add(new MyJetGunBullet { GunFireStart = true, X = _mj.X2, Y = _mj.Y2 - 13, Size = 3, DeltaY = -1 });   //DeltaY - 1 so bullet will move upwards
                            break;
                        }
                        case 3:
                        {
                            _mjg.Add(new MyJetGunBullet { GunFireStart = true, X = _mj.X2, Y = _mj.Y2 - 10, Size = 3 });
                            _mjg.Add(new MyJetGunBullet { GunFireStart = true, X = _mj.X2, Y = _mj.Y2 - 13, Size = 3, DeltaY = -1 });   
                            _mjg.Add(new MyJetGunBullet {GunFireStart = true, X = _mj.X2, Y = _mj.Y2 - 7, Size = 3,DeltaY = 1});        //DeltaY + 1 so bullet will move downwards
                            break;
                        }
                    }

                    _ki.KeyFire = false;
                }

                //jet drop bomb - only when bomb remains and no other bomb is being dropped
                if (_ki.KeyBomb && _mjb.BombRemains > 0 && _mjb.BombX == 0)
                {
                    _mjb.BombX = _mj.X2;
                    _mjb.BombY = _mj.Y2;
                    _mjb.BombRemains--;
                }

                //Draw bomb drop
                if (_mjb.BombX > 0) //Only when bomb is away
                {
                    if (_gc.EnableSound)    //Play bomb away sound
                    {
                        var spJetBomb = new MediaPlayer();
                        spJetBomb.Open(new Uri(@"G:\VSprojects\FlyingGame\FlyingGame\Sounds\BombAway.wav"));
                        spJetBomb.Play();
                    }

                    sky.FillEllipse(Brushes.Orange, new Rectangle { Height = 5, Width = 5, X = _mjb.BombX, Y = _mjb.BombY });

                    _mjb.BombY = _mjb.BombY + _gc.MyJetBombDelta;       //move bomb down for next round

                    if (_mjb.BombY > PbConsole.Height) ExplodeBomb();   //If bomb touched the ground, explode it
                }

                //move jet horizontally

                if (_ki.KeyDirectionHorizontal > 0)
                {
                    if (_ki.KeyDirectionHorizontal == 39)
                        _mj.RefX = _mj.RefX + _gc.MyJetDelta;

                    if (_ki.KeyDirectionHorizontal == 37)
                        _mj.RefX = _mj.RefX - _gc.MyJetDelta;
                }

                if (_ki.KeyDirectionVertical > 0)
                {
                    //Move jet vertically
                    if (_ki.KeyDirectionVertical == 40)
                    {
                        _mj.RefY = _mj.RefY + _gc.MyJetDelta;
                        _mj.MovementState = -1;                     //Set state will draw jet based for going up state
                    }
                    if (_ki.KeyDirectionVertical == 38)
                    {
                        _mj.RefY = _mj.RefY - _gc.MyJetDelta;
                        _mj.MovementState = 1;                      //Set state to dictate jet for going down state
                    }
                }
                else
                {
                    _mj.MovementState = 0;                          //Default state - jet is vertically stationery
                }

                //Keep the jet within canvas
                if (_mj.RefY < 0)
                    _mj.RefY = 0;
                if (_mj.Y2 > PbConsole.Height)
                    _mj.RefY = PbConsole.Height - (_mj.Y2 - _mj.RefY);
                if (_mj.RefX < 0)
                    _mj.RefX = 0;
                if (_mj.X2 > PbConsole.Width)
                    _mj.RefX = PbConsole.Width - (_mj.X2 - _mj.RefX);


                //draw jet
                switch (_mj.MovementState)
                {
                    case 0:
                    {
                        sky.FillPolygon(Brushes.Gold, _mj.FuselagePosOne(_mj.RefX, _mj.RefY));
                        sky.FillPolygon(Brushes.DodgerBlue, _mj.WingPosOne(_mj.RefX, _mj.RefY));
                        sky.FillPolygon(Brushes.DodgerBlue, _mj.TailPosOne(_mj.RefX, _mj.RefY));
                        break;
                    }
                    case 1:
                    {
                        sky.FillPolygon(Brushes.Gold, _mj.FuselagePosOne(_mj.RefX, _mj.RefY));
                        sky.FillPolygon(Brushes.DodgerBlue, _mj.LefWingPosTwo(_mj.RefX, _mj.RefY));
                        sky.FillPolygon(Brushes.DodgerBlue, _mj.RightWingPosTwo(_mj.RefX, _mj.RefY));
                        sky.FillPolygon(Brushes.DodgerBlue, _mj.TailPosTwo(_mj.RefX, _mj.RefY));
                        break;
                    }
                    case -1:
                    {
                        sky.FillPolygon(Brushes.Gold, _mj.FuselagePosOne(_mj.RefX, _mj.RefY));
                        sky.FillPolygon(Brushes.DodgerBlue, _mj.LefWingPosThree(_mj.RefX, _mj.RefY));
                        sky.FillPolygon(Brushes.DodgerBlue, _mj.RightWingPosThree(_mj.RefX, _mj.RefY));
                        sky.FillPolygon(Brushes.DodgerBlue, _mj.TailPosThree(_mj.RefX, _mj.RefY));
                        break;
                    }
                }

                //draw jetexhaust burner
                if (_mj.JetBurnerController%3 == 0) //every after 3 timer.ticks
                {
                    sky.FillPolygon(Brushes.Orange, _mj.JetBurner(_mj.RefX, _mj.RefY));
                }
                else
                {
                    sky.FillPolygon(Brushes.Red, _mj.JetBurner(_mj.RefX, _mj.RefY));
                }

                _mj.JetBurnerController++;
                if (_mj.JetBurnerController > 5)
                    _mj.JetBurnerController = 0;

                //animate jet gunfire
                if (_mjg.Select(x => x).Where(x => x.GunFireStart).Count() > 0)
                {
                    for (int i = 0; i < _mjg.Count; i++)
                    {
                        //Draw bullet
                        sky.DrawEllipse(_myJetBulletColor, _mjg[i].X, _mjg[i].Y, _mjg[i].Size, _mjg[i].Size);
                        
                        //Move bullet for next turn
                        _mjg[i].X = _mjg[i].X + 10;
                        _mjg[i].Y = _mjg[i].Y + _mjg[i].DeltaY;
                        
                        //Identy bullets out of window and mark for removal from list
                        if (_mjg[i].X > PbConsole.Width || _mjg[i].Y < 0 || _mjg[i].Y > PbConsole.Height)
                        {
                            _mjg[i].GunFireStart = false;
                        }

                        //Attack enemy
                        for (int j = 0; j < _ej.Count; j++)
                        {
                            if (_mjg[i].X + 10 >= _ej[j].X2 && _mjg[i].X <= _ej[j].RefX &&
                                ((_mjg[i].Y >= _ej[j].RefY && _mjg[i].Y <= _ej[j].Y2) ||
                                 (_mjg[i].Y + _mjg[i].Size >= _ej[j].RefY && _mjg[i].Y + _mjg[i].Size <= _ej[j].Y2)))
                            {
                                EnemyDestroyed(_mjg[i].X, _mjg[i].Y, false);                //Add an explosion in the list
                                _ej[j].MakeRedundant = true;                                //Mark the jet for removal from list
                                _mjg[i].GunFireStart = false;                               //Mark the bullet for removal from list

                                //play enemy destroy sound
                                if (_gc.EnableSound)
                                {
                                    var spEnemyXplode = new MediaPlayer();
                                    spEnemyXplode.Open(
                                        new Uri(@"G:\VSprojects\FlyingGame\FlyingGame\Sounds\EnemyXplode.wav"));
                                    spEnemyXplode.Play();
                                }
                            }
                        }

                        //Attack boss
                        if (_eb.IsBossInitiated && _eb.CurrHp>0)    //only when boss is present and alive
                        {
                            if(_mjg[i].X + 10>=_eb.RefX && _mjg[i].X <=_eb.X2 &&
                                ((_mjg[i].Y>=_eb.HitY1 && _mjg[i].Y<=_eb.HitY2)||(_mjg[i].Y + 10>=_eb.HitY1 && _mjg[i].Y + 10 <=_eb.HitY2)))
                            {
                                _eb.CurrHp--;
                                EnemyDestroyed(_mjg[i].X, _mjg[i].Y, true);                 //Add an exposion in the list
                                _mjg[i].GunFireStart = false;                               //Mark the bullet for removal from list

                            }
                        }
                    }
                }
                
                #endregion

                #region EnemyJet

                //Initiate enemy                    
                if (_ej.Count < _gc.MaxEnemyPerLvl)                 //Only when current enemy cound is less than set limit        
                {
                    if (rand.Next(1, _gc.EnemyJetGenOdd) == 12)    //generate enemy object based on random selection and add in enemy list
                    {
                        _ej.Add(new EnemyJet{MovementState = 0,RefX = PbConsole.Width,RefY = rand.Next(5, PbConsole.Height)});
                    }
                }

                if (_ej.Count == 0) goto Skipped_ej;                //no enemy to process, skip this region

                //Move enemy
                for (int i = 0; i < _ej.Count; i++)
                {
                    //Select direction state randomly
                    switch (rand.Next(1, 15))
                    {
                        case 1: { _ej[i].Direction = 1; break; } //Down
                        case 2: { _ej[i].Direction = -1;break; } //Up
                        case 3: {_ej[i].Direction = 0; break; } //Back
                        
                        default:_ej[i].Direction = _ej[i].Direction;break;
                    }

                    //Keep enemy within canvas
                    if (_ej[i].RefY < 0) _ej[i].Direction = 1;

                    if (_ej[i].Y2 > PbConsole.Height) _ej[i].Direction = -1;

                    //Move enemy according to Direction
                    switch (_ej[i].Direction)
                    {
                        case 0: {_ej[i].RefX = _ej[i].RefX + 10; break;}
                        case 1: {_ej[i].RefY = _ej[i].RefY + 5; break;}
                        case -1: {_ej[i].RefY = _ej[i].RefY - 5; break;}
                    }

                    //Continuous Move toward left
                    _ej[i].RefX = _ej[i].RefX - 5;

                    //Update movement state to apply drawing shape accordingly
                    _ej[i].MovementState++;

                    if (_ej[i].MovementState%3 == 0)    //after 3 timer.tick change state
                        _ej[i].TwistEnemy = true;

                    if (_ej[i].MovementState%5 == 0)    //after 3 timer.tick change state back
                        _ej[i].TwistEnemy = false;

                    if (_ej[i].MovementState > 6)
                        _ej[i].MovementState = 0;       //Do not allow movementState counter to go more than 6 count

                    //Draw enemy
                    sky.FillPolygon(Brushes.Tomato, _ej[i].FuselagePosOne(_ej[i].RefX, _ej[i].RefY));

                    if (_ej[i].TwistEnemy)
                    {
                        sky.FillPolygon(Brushes.DarkSlateGray,
                            _ej[i].WingBottomPosTwo(_ej[i].RefX, _ej[i].RefY));
                        sky.FillPolygon(Brushes.DarkSlateGray,
                            _ej[i].WingTopPosTwo(_ej[i].RefX, _ej[i].RefY));
                    }
                    else
                    {
                        sky.FillPolygon(Brushes.DarkSlateGray,
                            _ej[i].WingTopPosOne(_ej[i].RefX, _ej[i].RefY));
                        sky.FillPolygon(Brushes.DarkSlateGray,
                            _ej[i].WingBottomPosOne(_ej[i].RefX, _ej[i].RefY));
                    }

                    //destroy my jet & enemy jet on collision (when godmode is false)
                    if (!_gc.GodMode)                                                   
                    {
                        if (_ej[i].X2 <= _mj.X2 && _ej[i].RefX >= _mj.RefX
                            && ((_ej[i].RefY >= _mj.RefY && _ej[i].RefY <= _mj.Y2) ||
                                (_ej[i].Y2 >= _mj.RefY && _ej[i].Y2 <= _mj.Y2)))
                        {
                            _ej[i].MakeRedundant = true;                                //Mark enemy jet for removal from list
                            _exp.Add(new Explosion
                            {
                                Colour = new Pen(Color.Red),
                                VisibilityCounter = 1,
                                Points = Explosion.Explode(_mj.RefX, _mj.RefY)
                            });
                            _exp.Add(new Explosion
                            {
                                Colour = new Pen(Color.Green),
                                VisibilityCounter = 1,
                                Points = Explosion.Explode(_ej[i].X2, _ej[i].Y2)
                            });

                            if (_gc.EnableSound)                                        //make sound
                            {
                                var spEnemyDestroyed = new MediaPlayer();
                                spEnemyDestroyed.Open(
                                    new Uri(@"G:\VSprojects\FlyingGame\FlyingGame\Sounds\EnemyXplode.wav"));
                                spEnemyDestroyed.Play();
                            }
                            
                            _mj.Hp--;                                                   //Reduce my jet's hp by 1
                            
                            if(_mj.Hp<=0) GameOver();                                   //If my hp reaches 0, game over
                        }
                    }

                    //get rid of enemy which are out of window
                    if (_ej[i].X2 < 0) _ej[i].MakeRedundant = true;

                    //Fire enemy gun - Based on random number selection, add bullet object in enemy bullet list.
                    if (rand.Next(1, _gc.EnemyBulletGenOdd) == 15)
                    {
                        _ejg.Add(new EnemyJetGunBullet{GunFireStart = true,Size = 10,X = _ej[i].X2,Y = _ej[i].Y2 - 3,IsBossGun = false});
                    }
                }

                Skipped_ej:

                //animate enemy gunfire
                if (_ejg.Count == 0) goto Powerups; //no enemy bullet to process, skip this region

                for (int i = 0; i < _ejg.Count; i++)
                {
                    if (!_ejg[i].IsBossGun)
                    {
                        sky.FillEllipse(Brushes.Red, new Rectangle {Height = _ejg[i].Size, Width = _ejg[i].Size, X = _ejg[i].X, Y = _ejg[i].Y});

                        _ejg[i].X = _ejg[i].X - _gc.EnemyJetBulletDelta; //Move bullet for next round drawing
                    }

                    //Attack my jet
                    if (!_gc.GodMode)
                    {
                        if (_ejg[i].X <= _mj.X2 && (_ejg[i].X + _ejg[i].Size) >= _mj.RefX &&
                           ((_ejg[i].Y >= _mj.RefY && _ejg[i].Y <= _mj.Y2) ||
                           (_ejg[i].Y + _ejg[i].Size >= _mj.RefY && _ejg[i].Y + _ejg[i].Size <= _mj.Y2)))
                        {
                            _exp.Add(new Explosion { Colour = new Pen(Color.Red), VisibilityCounter = 1, Points = Explosion.Explode(_ejg[i].X, _ejg[i].Y) });   //Add new explosion in Red (because it's my jet) in list
                            
                            _mj.Hp--;                           //Reduce my jet's hp by 1;                        

                            _ejg[i].GunFireStart = false;       //Mark enemy bullet for removal from the list
                            if (_mj.Hp <= 0) GameOver();        //If my jet's hp reaches 0, game over
                        }
                    }

                    //Mark out of the window enemy bullet for removal from the list
                    if (_ejg[i].X < 0) _ejg[i].GunFireStart = false;
                }
                
                #endregion

                #region PowerUps
            Powerups:

                if (_pu.PowerUpType == 0 || _pu.PowerUpType > 3)    
                {
                    //initiate
                    _pu.PowerUpType = rand.Next(1, _gc.PowerUpGenOdd);
                    
                    //Reset power up type if max reached
                    if ((_mj.CurrentActiveGun == 3 && _pu.PowerUpType == 3) ||
                        (_mj.CurrentBulletLimit == 10 && _pu.PowerUpType == 2) ||
                        (_mjb.BombRemains == 5 && _pu.PowerUpType == 1))
                        _pu.PowerUpType = 0;

                    if (_pu.PowerUpType > 3)
                    {
                        _pu.PowerUpType = 0;
                    }
                    else
                    {
                        _pu.X = PbConsole.Width;                                    //Start on the far right
                        _pu.Y = rand.Next(1, PbConsole.Height);                     //Anywhere between top and bottom
                        if (_pu.Y > PbConsole.Height/2) _pu.IsTopToBottom = false;  //Set initial direction based on Y value
                    }
                }

                //Process power up
                if (_pu.PowerUpType == 0) goto ExplosionRegion;     //No power up exist, skip this region

                //Boundary bounce direction
                if (_pu.Y < 0)                              //Hit top wall, change direction to bottom
                {
                    _pu.IsTopToBottom = true;
                    _pu.BounceCounter++;
                }
                if (_pu.Y + _pu.Size > PbConsole.Height)    //Hit bottom wall, change direction to top
                {
                    _pu.IsTopToBottom = false;
                    _pu.BounceCounter++;
                }
                if (_pu.X < 0)                              //Hit left wall, change direction to right
                {
                    _pu.IsRightToLeft = false;
                    _pu.BounceCounter++;
                }
                if (_pu.X + _pu.Size > PbConsole.Width)     //Hit right wall, change direction to left
                {
                    _pu.IsRightToLeft = true;
                    _pu.BounceCounter++;
                }
                
                //Progress move based on direction
                _pu.X = _pu.IsRightToLeft ? _pu.X - _pu.DeltaX:_pu.X + _pu.DeltaX;
                _pu.Y = _pu.IsTopToBottom ? _pu.Y + _pu.DeltaY : _pu.Y - _pu.DeltaY;

                //Draw
                sky.FillRectangle(_pu.PowerUpColour, new Rectangle(_pu.X, _pu.Y, _pu.Size, _pu.Size));

                //PowerUp my jet on contact
                if (_mj.X2 >= _pu.X && _mj.RefX <= _pu.X + _pu.Size &&
                    ((_mj.RefY >= _pu.Y && _mj.RefY <= _pu.Y + _pu.Size) ||
                     (_mj.Y2 >= _pu.Y && _mj.Y2 <= _pu.Y + _pu.Size)))
                {
                    switch (_pu.PowerUpType)
                    {
                        case 1: {_mjb.BombRemains++;break;}
                        case 2:{_mj.CurrentBulletLimit++;break;}
                        case 3:{_mj.CurrentActiveGun++;break;}
                    }

                    //play power up sound
                    if (_gc.EnableSound)
                    {
                        var spPowerUp = new MediaPlayer();
                        spPowerUp.Open(new Uri(@"G:\VSprojects\FlyingGame\FlyingGame\Sounds\tada.wav"));
                        spPowerUp.Play();
                    }

                    //reset powerup object for next one
                    _pu = new PowerUps();
                }
                
                //Change blink
                _pu.Blink = !_pu.Blink;

                //Reset poewr up after 25 bounces
                if (_pu.BounceCounter >= 25) _pu = new PowerUps();

                #endregion

                #region Explosion Draw

                ExplosionRegion:

                //Draw explosions
                if (_exp.Count == 0) goto BossRegion; //nothing to explode, skip this region
                foreach (var explostions in _exp)
                {
                    sky.DrawPolygon(explostions.Colour, explostions.Points);
                    explostions.VisibilityCounter++;
                    if (explostions.VisibilityCounter > _gc.ExplosionVisibleMaxTime)
                        explostions.VisibilityCounter = 0;  //Mark exposion for removal from list
                }

                #endregion
            
                #region Boss
            
            BossRegion:
                if (!_eb.IsBossInitiated) goto BombExploded;            //Boss is not initated yet, skip this region

                //Initiate boss object
                if (_eb.RefX == 0 && _eb.RefY == 0)
                {
                    _eb.RefX = PbConsole.Width;                         //start all the way to right
                    _eb.RefY = rand.Next(0, PbConsole.Height);          //Start anywhere in between top and bottom
                }
                _eb.PrevDirection = _eb.Direction;                      //Get previous direction
                _eb.Direction = (byte)rand.Next(0, 15);                 //Get a random direction 
                
                if (_eb.Direction > 4 || _eb.Direction < 1)             
                    _eb.Direction = _eb.PrevDirection;                  //Re-instate previous direction of new direction is not between 1 to 4

                //boundary bounce for top and bottom ceiling
                if (_eb.RefY < 0)
                    _eb.Direction = 3;
                if (_eb.RefY + _eb.Width > PbConsole.Height)
                    _eb.Direction = 1;

                switch (_eb.Direction)
                {
                    case 1: { _eb.RefY = _eb.RefY - _eb.Delta; break; } //Move up
                    case 2: { _eb.RefX = _eb.RefX - _eb.Delta; break; } //Move left
                    case 3: { _eb.RefY = _eb.RefY + _eb.Delta; break; } //Move down
                    case 4: { _eb.RefX = _eb.RefX + _eb.Delta; break; } //Move right
                }

                //move to let till the whole boss appears in screen
                if (_eb.X2 > PbConsole.Width)
                    _eb.RefX = _eb.RefX - 3;

                //Update boss hp display
                LblBossRem.Text = _eb.CurrHp + @"/" + _eb.OriginalHp;

                //Process hp status
                if (_eb.CurrHp <= _eb.HitPerLevel * 2 && _eb.CurrHp > _eb.HitPerLevel && _eb.CurrHitLevel==1) //increase boss gun fire to level 2
                {
                    _eb.CurrHitLevel++;
                    _eb.FireLvlBigGun++;
                    _eb.FireLvlMiniGun++;
                }

                if (_eb.CurrHp < _eb.HitPerLevel && _eb.CurrHitLevel < 3) //increase boss gun fire to level 3
                {
                    _eb.CurrHitLevel++;
                    _eb.FireLvlBigGun++;
                    _eb.FireLvlMiniGun++;
                }

                if (_eb.CurrHp <= 0) //boss has been destroyed
                {
                    BackColor = Color.Empty;

                    _eb.BossDestroyedFireWorkCounter++;
                    
                    //Draw boss with explosion
                    
                    sky.FillPolygon(Brushes.DodgerBlue, _eb.WingMiddle());
                    sky.FillPolygon(Brushes.RoyalBlue, _eb.FuselageBottom());
                    sky.FillPolygon(Brushes.CadetBlue, _eb.FuselageTop());
                    sky.FillPolygon(Brushes.Teal, _eb.TopFin());
                    sky.FillPolygon(Brushes.MidnightBlue, _eb.BackExhaust());

                    sky.DrawPolygon(new Pen(Color.Black), _eb.FuselageMain());
                    sky.DrawPolygon(new Pen(Color.Black), _eb.FuselageBottom());
                    sky.DrawPolygon(new Pen(Color.Black), _eb.FuselageTop());
                    sky.DrawPolygon(new Pen(Color.Black), _eb.TopFin());

                    
                    if (_eb.BossDestroyedFireWorkCounter%2 == 0)
                    {
                        _exp.Add(new Explosion {Colour = new Pen(Color.Green),VisibilityCounter = 1,Points = Explosion.Explode(_eb.RefX + _eb.BossDestroyedFireWorkCounter, _eb.HitY1 - 10)});
                        sky.FillPolygon(Brushes.Red, _eb.FuselageMain());

                        //continue making sound
                        if (_gc.EnableSound)    
                        {
                            var spBossDestroyed1 = new MediaPlayer();
                            spBossDestroyed1.Open(new Uri(@"G:\VSprojects\FlyingGame\FlyingGame\Sounds\BombXplode.wav"));
                            spBossDestroyed1.Play();
                        }
                    }
                    else
                    {
                        _exp.Add(new Explosion { Colour = new Pen(Color.Teal), VisibilityCounter = 1, Points = Explosion.Explode(_eb.RefX + _eb.BossDestroyedFireWorkCounter, _eb.HitY2-10)});
                        sky.FillPolygon(Brushes.OrangeRed, _eb.FuselageMain());

                        //continue making sound
                        if (_gc.EnableSound)    
                        {
                            var spBossDestroyed2 = new MediaPlayer();
                            spBossDestroyed2.Open(new Uri(@"G:\VSprojects\FlyingGame\FlyingGame\Sounds\EnemyXplode.wav"));
                            spBossDestroyed2.Play();
                        }
                    }
                    
                    if (_eb.BossDestroyedFireWorkCounter >= 100)
                    {
                        //Enough boss destroy fireworks, put the show to an end
                        _eb = new EnemyBoss();
                        LblBoss.Visible = false;
                        LblBossRem.Visible = false;
                        _gc.BossAppearScore = (int) (_gc.BossAppearScore + _gc.BossAppearInterval * 1.5);
                        _gc.LevelCompleted++;
                        _eb.CurrHp = _eb.CurrHp + _gc.BossHpIncrement*_gc.LevelCompleted;
                        _eb.OriginalHp = _eb.CurrHp + _gc.BossHpIncrement * _gc.LevelCompleted;
                    }
                }
                else
                {   //Boss is still there
                    //Draw boss
                    switch (_eb.CurrHitLevel) //Change fuselate colour based on hit level
                    {
                        case 1:{sky.FillPolygon(Brushes.Silver, _eb.FuselageMain());break;}
                        case 2:{sky.FillPolygon(Brushes.LightPink, _eb.FuselageMain());break;}
                        case 3:{sky.FillPolygon(Brushes.Orange, _eb.FuselageMain());break;}
                    }
                    
                    //Rest of the body 
                    sky.DrawPolygon(new Pen(Color.Black), _eb.FuselageMain());
                    sky.DrawPolygon(new Pen(Color.Black), _eb.FuselageBottom());
                    sky.DrawPolygon(new Pen(Color.Black), _eb.FuselageTop());
                    sky.DrawPolygon(new Pen(Color.Black), _eb.TopFin());

                    sky.FillPolygon(Brushes.DodgerBlue, _eb.WingMiddle());
                    sky.FillPolygon(Brushes.RoyalBlue, _eb.FuselageBottom());
                    sky.FillPolygon(Brushes.CadetBlue, _eb.FuselageTop());
                    sky.FillPolygon(Brushes.Teal, _eb.TopFin());
                    sky.FillPolygon(Brushes.MidnightBlue, _eb.BackExhaust());
                    sky.FillPolygon(Brushes.DarkRed, _eb.MiniGunTop());
                    sky.FillPolygon(Brushes.DarkRed, _eb.MiniGunBottom());
                    sky.FillPolygon(Brushes.Red, _eb.BigGun());

                    //Back burner draw
                    if (_eb.ToggleBurner)
                    {sky.FillPolygon(Brushes.Tomato, _eb.BackBurner());}
                    else
                    {sky.FillPolygon(Brushes.Yellow, _eb.BackBurner());}
                }

                _eb.ToggleBurner = !_eb.ToggleBurner;

                //Destroy my jet if collide with boss
                if (!_gc.GodMode)
                {
                    if (_eb.RefX <= _mj.X2 && _eb.X2 >= _mj.RefX
                        && ((_eb.RefY >= _mj.RefY && _eb.RefY <= _mj.Y2) ||
                            (_eb.HitY2 >= _mj.RefY && _eb.HitY2 <= _mj.Y2)))
                    {
                        _exp.Add(new Explosion{Colour = new Pen(Color.Red),VisibilityCounter = 1,Points = Explosion.Explode(_mj.RefX, _mj.RefY)});

                        _mj.Hp--;

                        if (_mj.Hp <= 0) GameOver();
                    }
                }

                if(_eb.CurrHp<=0) goto SkipBossFire;    //Boss is in destruction animation mode, skip this region

                //Fire mini gun 1
                if (rand.Next(0, _gc.BossMiniGunBulletGenOdd / _eb.FireLvlMiniGun) == 1)
                {
                    _ejg.Add(new EnemyJetGunBullet { GunFireStart = true, IsBossGun = true, X = _eb.MiniGun1X, Y = _eb.MiniGun1Y, Size = _gc.BossMiniGunBulletSize, InitialX = _eb.MiniGun1X, InitialY = _eb.MiniGun1Y, MyJetX = _mj.X2, MyJetY = _mj.Y2 - 5, DeltaX = 8 });
                }

                //Fire mini gun 2
                if (rand.Next(0, _gc.BossMiniGunBulletGenOdd / _eb.FireLvlMiniGun) == 2)
                {
                    _ejg.Add(new EnemyJetGunBullet { GunFireStart = true, IsBossGun = true, X = _eb.MiniGun2X, Y = _eb.MiniGun2Y, Size = _gc.BossMiniGunBulletSize, InitialX = _eb.MiniGun2X, InitialY = _eb.MiniGun2Y, MyJetX = _mj.X2, MyJetY = _mj.Y2 - 5, DeltaX = 8 });
                }

                //Fire big gun
                if (rand.Next(0, _gc.BossBigGunBulletGenOdd / _eb.FireLvlMiniGun) == 5)
                {
                    _ejg.Add(new EnemyJetGunBullet { GunFireStart = true, IsBossGun = true, X = _eb.BigGunX, Y = _eb.BigGunY, Size = _gc.BossBigGunBulletSize, InitialX = _eb.BigGunX, InitialY = _eb.BigGunY, MyJetX = _mj.X2, MyJetY = _mj.Y2 - 5, DeltaX = 7, IsBigGun = true });
                }
               
                //Animate boss bullets
                foreach (var bullet in _ejg)
                {
                    if (bullet.IsBossGun)
                    {
                        if (bullet.IsBigGun)
                        {sky.DrawEllipse(new Pen(Color.Red), new Rectangle(bullet.X, bullet.Y, bullet.Size, bullet.Size));}
                        else
                        {sky.FillEllipse(Brushes.DarkRed, new Rectangle(bullet.X, bullet.Y, bullet.Size, bullet.Size));}
                        
                        bullet.X = bullet.X - bullet.DeltaX;
                        bullet.Y = bullet.Y + (int)bullet.DeltaGunTargetY;

                        //Identify bullets that left window
                        if (bullet.X < 0||bullet.Y<0||bullet.Y>PbConsole.Height)
                            bullet.GunFireStart = false;

                        //Destroy jet
                        if (!_gc.GodMode)
                        {
                            if (((bullet.X <= _mj.X2 && bullet.X >= _mj.RefX) || (bullet.X + bullet.Size <= _mj.X2 && bullet.X + bullet.Size >= _mj.RefX))
                                &&
                               ((bullet.Y >= _mj.RefY && bullet.Y <= _mj.Y2) || (bullet.Y + bullet.Size >= _mj.RefY && bullet.Y + bullet.Size <= _mj.Y2)))
                            {
                                _exp.Add(new Explosion{Colour = new Pen(Color.Red),VisibilityCounter = 1,Points = Explosion.Explode(bullet.X, bullet.Y)});

                                if (bullet.IsBigGun)
                                {
                                    _mj.Hp = (sbyte) (_mj.Hp - 4);      //Big gun damage 4 hp
                                }
                                else
                                {
                                    _mj.Hp = (sbyte) (_mj.Hp - 2);      //Small gun damage 2 hp
                                }
                                
                                bullet.GunFireStart = false;

                                if (_mj.Hp <= 0) GameOver();            //My jet's hp reached 0, game over
                            }
                        }
                    }
                }

            SkipBossFire:

                #endregion

                #region BombExploded

            BombExploded:
                if (!_mjb.BombXploded) goto Cleanup;    //No bomb to explode, skip this region

                _mjb.BombExplodedDisplayTimer++;

                if (_mjb.BombExplodedDisplayTimer == 1) //The moment bomb exploded..
                {
                    //Draw explosions for all the enemy jets and add scores for each jet
                    foreach (var enemy in _ej)
                    {
                        sky.FillPolygon(Brushes.Green, Explosion.Explode(enemy.RefX, enemy.RefY));
                        _gc.Score = _gc.Score + 10;
                    }
                    //Draw explosions for all the enemy bullets and add scores for each bullet
                    foreach (var enemyFire in _ejg)
                    {
                        sky.FillPolygon(Brushes.Green, Explosion.Explode(enemyFire.X, enemyFire.Y));
                        _gc.Score = _gc.Score + 1;
                    }
                    //do boss hp damage by 10% of full hp value
                    if (_eb.IsBossInitiated) 
                    {
                        _eb.CurrHp = _eb.CurrHp - _gc.BossHp/10;
                    }
                }

                LblScore.Text = _gc.Score.ToString();

                if (_mjb.BombExplodedDisplayTimer > _gc.BombExplosionVisibleTimeMax)
                {
                    //Reset bomb exploded so rest can continue 
                    _mjb.BombXploded = false;
                    _mjb.BombExplodedDisplayTimer = 0;
                    //Clear all enemy jet and bullet list
                    _ej.Clear();
                    _ejg.Clear();
                }

                #endregion
                
                #region Cleanup
            Cleanup:
                //remove redundant object drawings
                for (int i = 0; i < _cL.Count; i++)
                {
                    if (!_cL[i].CloudInitiated) _cL.Remove(_cL[i]);
                }
                
                for (int i = 0; i < _mjg.Count; i++)
                {
                    if (!_mjg[i].GunFireStart) _mjg.Remove(_mjg[i]);
                }

                for (int i = 0; i < _ej.Count; i++)
                {
                    if (_ej[i].MakeRedundant) _ej.Remove(_ej[i]);
                }

                for (int i = 0; i < _ejg.Count; i++)
                {
                    if (!_ejg[i].GunFireStart) _ejg.Remove(_ejg[i]);
                }

                for (int i = 0; i < _exp.Count; i++)
                {
                    if (_exp[i].VisibilityCounter == 0) _exp.Remove(_exp[i]);
                }

                #endregion

                EndRoutine();

                LevelAndHpUpdate();
            }
        }

        private void StartRoutine()
        {
            LblScore.Text = _gc.Score.ToString();
            LblJetHp.Text = _mj.Hp.ToString();
            LblBombRem.Text = _mjb.BombRemains.ToString();
            LblFireLimit.Text = _mj.CurrentBulletLimit.ToString();
            LblFireLine.Text = _mj.CurrentActiveGun.ToString();
        
        }

        private void EndRoutine()
        {
            if (_mj.Hp < 0) _mj.Hp = 0;
            if (_eb.CurrHp < 0) _eb.CurrHp = 0;
            if (_gc.EnemyJetGenOdd < 13) _gc.EnemyJetGenOdd = 13;
            if (_gc.EnemyBulletGenOdd < 16) _gc.EnemyBulletGenOdd = 16;
            if (_gc.BossMiniGunBulletGenOdd < 2) _gc.BossMiniGunBulletGenOdd = 2;
            if (_gc.BossBigGunBulletGenOdd < 6) _gc.BossBigGunBulletGenOdd = 6;
        }

        private void GameOver()
        {
            _gc.IsGameOver = true;
            var jetDestroyed = new MediaPlayer();
            jetDestroyed.Open(new Uri(@"G:\VSprojects\FlyingGame\FlyingGame\Sounds\EnemyXplode.wav"));
            jetDestroyed.Play();
            LblStartEndBanner.Visible = true;
            LblStartEndBanner.Text = @"Game Over!!! Hit enter to start another game.";
            LblStartEndBanner.ForeColor = Color.Red;
            LblInfo.Visible = true;
        }

        private void ExplodeBomb()
        {
            if (_gc.EnableSound)
            {
                var spBombXplode = new MediaPlayer();
                spBombXplode.Open(new Uri(@"G:\VSprojects\FlyingGame\FlyingGame\Sounds\BombXplode.wav"));
                spBombXplode.Play();
            }
            
            _mjb.BombXploded = true; //This will freeze all the actions except bomb explosions from next round till set time.
            _mjb.BombX = 0;
            _mjb.BombY = 0;
        }

        private void LevelAndHpUpdate()
        {
            if (!_eb.IsBossInitiated)
            {
                if (_gc.Score >= _gc.NextScoreCheckPoint)
                {
                    _gc.NextScoreCheckPoint = _gc.NextScoreCheckPoint + 100;    //update next score check point
                    _gc.MaxEnemyPerLvl++;                                       //increase enemy per level limit
                    _mj.Hp++;                                                   //inrease my jet Hp
                }
            }

            if (_gc.Score > _gc.BossAppearScore)
            {
                BossFight();
            }
        }

        private void BossFight()
        {
            _gc.MaxEnemyPerLvl = 1; //Reduce enemy jets
            _eb.IsBossInitiated = true; //This will initiate boss fight animation from next round till the boss is destroyed
            LblBoss.Visible = true;
            LblBossRem.Visible = true;
            BackColor = Color.Red;
        }

        private void EnemyDestroyed(int x, int y, bool isBoss)
        {
            if (!isBoss) _gc.Score = _gc.Score + 10;
            
            LblScore.Text = _gc.Score.ToString();
            _exp.Add(new Explosion { Colour = new Pen(Color.Green), VisibilityCounter = 1, Points = Explosion.Explode(x, y) });
        }

        private void MainConsole_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Up))
                _ki.KeyDirectionVertical = 38;

            if (Keyboard.IsKeyDown(Key.Down))
                _ki.KeyDirectionVertical = 40;

            if (Keyboard.IsKeyDown(Key.Left))
                _ki.KeyDirectionHorizontal = 37;
            
            if (Keyboard.IsKeyDown(Key.Right))
                _ki.KeyDirectionHorizontal = 39;

            if (Keyboard.IsKeyDown(Key.A))
                _ki.KeyFire = true;

            if (Keyboard.IsKeyDown(Key.D))
                _ki.KeyBomb = true;

            if (Keyboard.IsKeyDown(Key.Enter))
                _ki.RestartGame = true;
        }

        private void MainConsole_KeyUp(object sender, KeyEventArgs e)
        {
            _ki.KeyDirectionVertical = 0;
            _ki.KeyDirectionHorizontal = 0;
            _ki.KeyFire = false;
            _ki.KeyBomb = false;
        }
    }
}
