
//Game
import java.util.*;
//import cassette.audiofiles.SoundFile;
import ketai.data.*;
import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorManager;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;

//Bluetooth
import android.os.Bundle;
import android.content.Intent;    
import ketai.net.bluetooth.*;
import ketai.ui.*; 
import ketai.net.*;
import oscP5.*;

//Sensors
SensorManager manager;
Sensor sensor;
AccelerometerListener listener;

//Bluetooth
KetaiBluetooth bt;
KetaiList connectionList;
PVector multiplayerPos  = new PVector();
PVector relativePos = new PVector();
PVector realPos = new PVector();
boolean paired = false;
boolean triggerPlay = false;
boolean ready = false;
boolean winner = true;

//Game
Context context;
//SoundFile collectSound, shieldSound, overSound, bgSound, slowSound, recordSound;
KetaiSQLite db;
boolean b = false;
boolean slowing = false;
PImage ClearIcon;
PImage RollIcon;
PImage RotateIcon;
PImage BluetoothIcon;
PImage SettingsIcon;
PImage LeftIcon;
PImage HomeIcon;
boolean RollOrRotate = true;
boolean game = false, startMenu = true, endMenu = false, settingsMenu = false;
boolean glow = true;
boolean shieldActive = false;
boolean keyPress = false;
PVector startButtonSize;
float az;
float maxTilt = 90;
float tilt = 0;
float tiltDecay = 0.95;
PVector mid;
PVector dir;
color collectColor = color(255, 177, 54);
color obstacleColor = color(173, 73, 254);
color interfaceColor = color(0, 237, 126);
color notifyColor = color(255, 0, 115);
color shieldColor = color(2, 162, 255);
color playerColor = color(0, 237, 126);
color multiplayerColor = color(255, 255, 255);
color bgColor = color(18, 0, 27);
color bestColor = color(90, 90, 27);
ArrayList<Obstacle> obstacles;
ArrayList<Collect> collects;
ArrayList<Power> powers;
ArrayList<Particle> particles;
int obstaclesPerScr = 50;
int collectsPerScr = 10;
int shieldsPerScr = 4;
float playerSize;
PVector playerCenter;
float startSpeed = 4;
float speed;
float speedMult = 0.075;
float speedLimit = 20;
float speedLimitInc = 7;
float collectSpeedMult = 0.8;
float shieldSpeedMult = 0.6;
int fps = 63;
float scaleFactor;
float score = 0;
float scoreTimeFactor = 10;
float scoreSpeedFactor = 0.1;
float shieldGlow;
int shieldActiveDur = 10;
int shieldActiveStart = 0;
float glowRad;
float glowRadMax;
float glowScalar = 0.05;
float activeScalar = 1.5;
float particleDensity = 5;
float shields = 0;
float keyMult = 0.3;
float best;
float slowingMult = 0.8;

void setup() {

  size(1080,1920);
  orientation(PORTRAIT);
  //fullScreen();
  frameRate(fps);
  background(bgColor);
  smooth();
  noStroke();
  fill(255);
  //randomSeed(int(random(100)));
  randomSeed(1);
  //Bluetooth
  bt.start();
  
  //DB
  DBInit();
  best = GetBest();
  //SetBest(0);
  CreateUser("record");
  //println(GetBest());
  
  //Sounds
  //overSound = new SoundFile(this, "Over.mp3");
  /*recordSound = new SoundFile(this, "Record.wav");
  slowSound = new SoundFile(this, "Slow.wav");*/
  
  //Images
  RollIcon = loadImage("RollIcon.png");
  RotateIcon = loadImage("RotateIcon.png");
  BluetoothIcon = loadImage("BluetoothIcon.png");
  SettingsIcon = loadImage("SettingsIcon.png");
  LeftIcon = loadImage("LeftIcon.png");
  HomeIcon = loadImage("HomeIcon.png");
  ClearIcon = loadImage("ClearIcon.png");

  //println(width+" "+height);
  //scaleFactor = min(width, height);
  scaleFactor = 1080;

  startButtonSize = new PVector(scaleFactor/2, (scaleFactor/2)/5);

  mid = new PVector(1080/2, 1920/2);
  //speed /= fps/60;

  //Android Sensors
  context = getActivity();
  manager = (SensorManager)context.getSystemService(Context.SENSOR_SERVICE);
  sensor = manager.getDefaultSensor(Sensor.TYPE_GYROSCOPE);
  listener = new AccelerometerListener();
  manager.registerListener(listener, sensor, SensorManager.SENSOR_DELAY_GAME);

  //Player
  playerCenter = mid;
  playerSize = scaleFactor/20;
  
  //Effects
  shieldGlow = playerSize;
  glowRad = playerSize * 1.05;
  glowRadMax = playerSize * 1.3;
  
  //println(SettingsIcon.width, SettingsIcon.height);
}

void draw() {

  if (startMenu) {
    //Score
    background(bgColor);
    textSize(scaleFactor/10);
    textAlign(CENTER, CENTER);
    fill(collectColor);
    text("TOP: "+round(best), mid.x, mid.y*0.35);
    image(ClearIcon, mid.x-ClearIcon.width/2, mid.y*0.15-ClearIcon.width/2);
    
    //Info text
    /*textAlign(LEFT, TOP);
    textSize(scaleFactor/10);
    fill(255);
    text("paired: "+paired, 10, scaleFactor/7);
    text("triggerPlay: "+triggerPlay, 10, 0);*/
    
    //Play
    stroke(interfaceColor);
    noFill();
    ellipse(mid.x, mid.y, startButtonSize.x, startButtonSize.x);
    textSize(scaleFactor/15);
    fill(255);
    textAlign(CENTER, CENTER);
    if(paired && ready)
      text("WAITING...", mid.x, mid.y);
    else
      text("PLAY", mid.x, mid.y);
    
    //Controls
    image(RollIcon, scaleFactor/6, height-height/6);
    image(RotateIcon, width-scaleFactor/6-RotateIcon.width, height-height/6);
    strokeWeight(3);
    stroke(interfaceColor);
    noFill();
    if(RollOrRotate)
      ellipse(scaleFactor/6+RotateIcon.width/2, height-height/6+RotateIcon.width/2, RotateIcon.width, RotateIcon.width);
    else
      ellipse(width-scaleFactor/6-RotateIcon.width/2, height-height/6+RotateIcon.width/2, RotateIcon.width, RotateIcon.width);
      
    //Multiplayer
    if(bt.getConnectedDeviceNames().size() <= 0){
      paired = false;
      stroke(multiplayerColor);
    }
    else{
      paired = true;
    }
    image(BluetoothIcon, width/2-BluetoothIcon.width/2, height-height/6-BluetoothIcon.width);
    ellipse(width/2, height-height/6-BluetoothIcon.width/2, BluetoothIcon.width, BluetoothIcon.width);
  }
  else if (endMenu) {
    background(bgColor);
    stroke(interfaceColor);
    noFill();
    ellipse(mid.x, mid.y, startButtonSize.x, startButtonSize.x);
    textSize(scaleFactor/15);
    fill(255);
    textAlign(CENTER, CENTER);
    if(paired && ready)
      text("WAITING...", mid.x, mid.y);
    else if(paired)
      text("GO AGAIN!", mid.x, mid.y);
    else
      text("TRY AGAIN!", mid.x, mid.y);
    textSize(scaleFactor/10);
    if(paired){
      if(winner){
        fill(collectColor);
        text("WINNER!", mid.x, mid.y*0.35);
      } else {
        fill(obstacleColor);
        text("LOSER!", mid.x, mid.y*0.35);
      }
    }
    else{
      fill(obstacleColor);
      text("GAME OVER", mid.x, mid.y*0.35);
    }
    fill(collectColor);
    textSize(scaleFactor/5);
    text(round(score), mid.x, mid.y*1.75);
    image(SettingsIcon, width-SettingsIcon.width-scaleFactor/20, scaleFactor/20);
    image(HomeIcon, scaleFactor/20, scaleFactor/20);
    
    //Particles
    for (int i = 0; i < particles.size(); i++) {
      particles.get(i).Draw();
      if(particles.get(i).life <= 0)
        particles.remove(i);
    }
  }
  else if (settingsMenu){
    background(bgColor);
    image(RollIcon, mid.x-RollIcon.width-scaleFactor/6, mid.y-RollIcon.width/2);
    image(RotateIcon, mid.x+scaleFactor/6, mid.y-RollIcon.width/2);
    image(LeftIcon, scaleFactor/20, scaleFactor/20);
    strokeWeight(3);
    stroke(interfaceColor);
    noFill();
    if(RollOrRotate)
      ellipse(mid.x-scaleFactor/6-RotateIcon.width/2, mid.y, RotateIcon.width, RotateIcon.width);
    else
      ellipse(mid.x+scaleFactor/6+RotateIcon.width/2, mid.y, RotateIcon.width, RotateIcon.width);
  }
  else if (game) {
    
    //Clear
    //Color
     /*if(score < best){
       PVector colorInc = new PVector(score/best*(red(bestColor)-red(bgColor)), score/best*(green(bestColor)-green(bgColor)), 0);
       background(red(bgColor)+colorInc.x, green(bgColor)+colorInc.y, blue(bgColor)+colorInc.z);
     }
     else
       background(bestColor);*/
     background(bgColor);
     
    //Low FPS compensation
    float rate = fps/frameRate;
    rate *= (rate>1.05) ? 1.2 : 1;
    
    //Score
    score += ((1.0/60.0)*scoreTimeFactor*(1+((speed-startSpeed)*scoreSpeedFactor)))*rate;
    if(score > best && !b){
      RecordAnimation();
      b = true;
    }

    //Speed limit
    if(speed > speedLimit && !slowing){
      slowing = true;
      //slowSound.play();
    }
    
    //Speed mult
    if(slowing && speed > startSpeed){
      speed *= 1-(slowingMult/60);
    }
    else{
      if(slowing){
        slowing = false;
        speedLimit += speedLimitInc;
      }
      speed *= 1+(speedMult/60);
    }
    
    //Tilt decay
    if (!keyPress)
      tilt *= tiltDecay;

    //Direction, Movement step
    float dirX = (RollOrRotate) ? radians(tilt*(1)+90) : radians(tilt*(-1)+90);
    dir = new PVector(cos(dirX), sin(dirX));
    PVector mov = new PVector(dir.x*speed*rate, dir.y*speed*rate);
    realPos.add(new PVector(mov.x*(-1), (dir.y-1)*-1*speed*rate));
    relativePos.add(new PVector(mov.x, (dir.y-1)*speed*rate));
    //realPos.add(new PVector(mov.x*(-1), 0));
    
    sendData();

    //Collects
    for (Collect obj : collects) {
      obj.Move(mov);
      obj.DrawC();
    }

    //Powers
    for (Power obj : powers) {
      obj.Move(mov);
      obj.DrawP();
    }
    
    //Obstacles
    for (Obstacle obj : obstacles) {
      obj.Move(mov);
      obj.Draw();
    }
    
    //Particles
    for (int i = 0; i < particles.size(); i++) {
      particles.get(i).Draw();
      if(particles.get(i).life <= 0)
        particles.remove(i);
    }
    
    //Shield
    fill(shieldColor);
    noStroke();
    if(shieldActive){
      ShieldActive();
      
      textAlign(CENTER, CENTER);
      textSize(scaleFactor/15);
      text(round(shieldActiveDur-(millis()-shieldActiveStart)/1000), mid.x, (mid.y+playerSize/2)+(scaleFactor/10/2));
    }
    else if(shields > 0){
      ShieldGlow();
      
      if(shields > 2){
        textAlign(CENTER, CENTER);
        textSize(scaleFactor/15);
        text("!", mid.x, (mid.y+playerSize/2)+(scaleFactor/10/2));
      }
    }
    
    //Multiplayer character
    float x_ = multiplayerPos.x+relativePos.x;
    float y_ = multiplayerPos.y+relativePos.y;
    //Left
    if(x_+playerSize/2<0){
      //left top
      if(y_+playerSize/2<0){
        Triangle(new PVector(playerSize, playerSize), playerSize, "lefttop");
      }
      //left bot
      else if(y_-playerSize/2>height){
        Triangle(new PVector(playerSize, height-playerSize), playerSize, "leftbot");
      }
      //left
      else{
        Triangle(new PVector(playerSize, y_), playerSize, "left");
      }
    }
    //Right
    else if(x_-playerSize/2>width){
      //right top
      if(y_+playerSize/2<0){
        Triangle(new PVector(width-playerSize, playerSize), playerSize, "righttop");
      }
      //right bot
      else if(y_-playerSize/2>height){
        Triangle(new PVector(width-playerSize, height-playerSize), playerSize, "rightbot");
      }
      //right
      else{
        Triangle(new PVector(width-playerSize, y_), playerSize, "lefttop");
      }
    }
    else{
      //top
      if(y_+playerSize/2<0){
        Triangle(new PVector(x_, playerSize), playerSize, "top");
      }
      //bot
      else if(y_-playerSize/2>height){
        Triangle(new PVector(x_, height-playerSize), playerSize, "bot");
      }
      else{
        fill(multiplayerColor);
        noStroke();
        ellipse(multiplayerPos.x+relativePos.x, multiplayerPos.y+relativePos.y, playerSize, playerSize);
      }
    }
    
    //Player character
    fill(playerColor);
    noStroke();
    ellipse(mid.x, mid.y, playerSize, playerSize);
  
    //Score
    textAlign(CENTER, CENTER);
    textSize(scaleFactor/7);
    fill(collectColor);
    text(round(score), mid.x, height-scaleFactor/7);
    //text(frameRate, mid.x, height-scaleFactor/7);
    
    //Info text
    /*textAlign(LEFT, TOP);
    textSize(scaleFactor/9);
    fill(255);
    text(round(frameRate), 10, scaleFactor/9);
    text(rate+" : "+fps/frameRate, 10, 0);*/
    
    //Speed bar
    float yP = width*((speed-startSpeed)/(speedLimit-startSpeed));
    stroke(255);
    strokeWeight(1);
    line(0, height-3, yP, height-3);
    /*strokeWeight(1);
    fill(interfaceColor);
    ellipse(scaleFactor/200, yP, scaleFactor/50, scaleFactor/50);*/
    
    //Score bar
    if(score <= best){
      yP = width*(score/best); 
      stroke(collectColor);
      strokeWeight(1);
      line(0, height-1, yP, height-1);
      /*strokeWeight(1);
      fill(interfaceColor);
      ellipse(yP, height-scaleFactor/200, scaleFactor/50, scaleFactor/50);*/
    }
  }
}

void onResume() {
  super.onResume();
  if (manager != null) {
    manager.registerListener(listener, sensor, SensorManager.SENSOR_DELAY_GAME);
  }
}

void onPause() {
  super.onPause();
  if (manager != null) {
    manager.unregisterListener(listener);
  }
}