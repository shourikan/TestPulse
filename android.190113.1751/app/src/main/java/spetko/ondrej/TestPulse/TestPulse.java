package spetko.ondrej.TestPulse;

import processing.core.*; 
import processing.data.*; 
import processing.event.*; 
import processing.opengl.*; 

import java.util.*; 
import ketai.data.*; 
import android.content.Context; 
import android.hardware.Sensor; 
import android.hardware.SensorManager; 
import android.hardware.SensorEvent; 
import android.hardware.SensorEventListener; 

import java.util.HashMap; 
import java.util.ArrayList; 
import java.io.File; 
import java.io.BufferedReader; 
import java.io.PrintWriter; 
import java.io.InputStream; 
import java.io.OutputStream; 
import java.io.IOException; 

public class TestPulse extends PApplet {


//import cassette.audiofiles.SoundFile;







SensorManager manager;
Sensor sensor;
AccelerometerListener listener;

//TODO: textfield

Context context;
//SoundFile collectSound, shieldSound, overSound, bgSound, slowSound, recordSound;
KetaiSQLite db;
boolean b = false;
boolean slowing = false;
PImage ClearIcon;
PImage RollIcon;
PImage RotateIcon;
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
float tiltDecay = 0.95f;
PVector mid;
PVector dir;
int collectColor = color(255, 177, 54);
int obstacleColor = color(173, 73, 254);
int interfaceColor = color(0, 237, 126);
int notifyColor = color(255, 0, 115);
int shieldColor = color(2, 162, 255);
int playerColor = color(0, 237, 126);
int bgColor = color(18, 0, 27);
int bestColor = color(90, 90, 27);
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
float speedMult = 0.075f;
float speedLimit = 20;
float speedLimitInc = 7;
float collectSpeedMult = 0.8f;
float shieldSpeedMult = 0.6f;
int fps = 63;
float scaleFactor;
float score = 0;
float scoreTimeFactor = 10;
float scoreSpeedFactor = 0.1f;
float shieldGlow;
int shieldActiveDur = 10;
int shieldActiveStart = 0;
float glowRad;
float glowRadMax;
float glowScalar = 0.05f;
float activeScalar = 1.5f;
float particleDensity = 5;
float shields = 0;
float keyMult = 0.3f;
float best;
float slowingMult = 0.8f;

public void setup() {

  //size(640,480);
  
  frameRate(fps);
  background(bgColor);
  
  noStroke();
  fill(255);
  
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
  SettingsIcon = loadImage("SettingsIcon.png");
  LeftIcon = loadImage("LeftIcon.png");
  HomeIcon = loadImage("HomeIcon.png");
  ClearIcon = loadImage("ClearIcon.png");

  //println(width+" "+height);
  scaleFactor = min(width, height);

  startButtonSize = new PVector(scaleFactor/2, (scaleFactor/2)/5);

  mid = new PVector(width/2, height/2);
  speed /= fps/60;

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
  glowRad = playerSize * 1.05f;
  glowRadMax = playerSize * 1.3f;
  
  //println(SettingsIcon.width, SettingsIcon.height);
}

public void draw() {

  if (startMenu) {
    background(bgColor);
    textSize(scaleFactor/10);
    textAlign(CENTER, CENTER);
    fill(collectColor);
    text("TOP: "+round(best), mid.x, mid.y*0.35f);
    image(ClearIcon, mid.x-ClearIcon.width/2, mid.y*0.15f-ClearIcon.width/2);
    stroke(interfaceColor);
    noFill();
    ellipse(mid.x, mid.y, startButtonSize.x, startButtonSize.x);
    textSize(scaleFactor/15);
    fill(255);
    textAlign(CENTER, CENTER);
    text("PLAY", mid.x, mid.y);
    image(RollIcon, scaleFactor/6, height-height/6);
    image(RotateIcon, width-scaleFactor/6-RotateIcon.width, height-height/6);
    strokeWeight(3);
    stroke(interfaceColor);
    noFill();
    if(RollOrRotate)
      ellipse(scaleFactor/6+RotateIcon.width/2, height-height/6+RotateIcon.width/2, RotateIcon.width, RotateIcon.width);
    else
      ellipse(width-scaleFactor/6-RotateIcon.width/2, height-height/6+RotateIcon.width/2, RotateIcon.width, RotateIcon.width);
    
  }
  else if (endMenu) {
    background(bgColor);
    stroke(interfaceColor);
    noFill();
    ellipse(mid.x, mid.y, startButtonSize.x, startButtonSize.x);
    textSize(scaleFactor/15);
    fill(255);
    textAlign(CENTER, CENTER);
    text("TRY AGAIN!", mid.x, mid.y);
    textSize(scaleFactor/10);
    fill(obstacleColor);
    text("GAME OVER", mid.x, mid.y*0.35f);
    fill(collectColor);
    textSize(scaleFactor/5);
    text(round(score), mid.x, mid.y*1.75f);
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
    
    //Score
    score += (1.0f/60.0f)*scoreTimeFactor*(1+((speed-startSpeed)*scoreSpeedFactor));
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
      speed *= 1-(slowingMult/frameRate);
    }
    else{
      if(slowing){
        slowing = false;
        speedLimit += speedLimitInc;
      }
      speed *= 1+(speedMult/frameRate);
    }
    
    //Tilt decay
    if (!keyPress)
      tilt *= tiltDecay;

    //Direction, Movement step
    float dirX = (RollOrRotate) ? radians(tilt*(1)+90) : radians(tilt*(-1)+90);
    dir = new PVector(cos(dirX), sin(dirX));
    PVector mov = new PVector(dir.x*speed, dir.y*speed);

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
    textSize(scaleFactor/7);
    fill(255);
    //text(round(frameRate), 10, scaleFactor/7);
    //text(frameCount, 10, 0);*/
    
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

public void onResume() {
  super.onResume();
  if (manager != null) {
    manager.registerListener(listener, sensor, SensorManager.SENSOR_DELAY_GAME);
  }
}

public void onPause() {
  super.onPause();
  if (manager != null) {
    manager.unregisterListener(listener);
  }
}
public boolean inRect(float x, float y, float width, float height)  {
  if (mouseX >= x && mouseX <= x+width && 
      mouseY >= y && mouseY <= y+height) {
    return true;
  } else {
    return false;
  }
}

public boolean inCircle(float x, float y, float diameter) {
  float disX = x - mouseX;
  float disY = y - mouseY;
  if (sqrt(sq(disX) + sq(disY)) < diameter/2 ) {
    return true;
  } else {
    return false;
  }
}
class Collect extends Obstacle{
  
  Collect (){
    super();
  }

  public void DrawC (){
    if (pos.y-size/2 >= height){
      ResetPos();
    }
    else{ 
      //On screens
      if(pos.x+size/2 > 0 && pos.x-size/2 < width)
        if(pos.y+size/2 > 0 && pos.y-size/2 < height){
          
          float distance = dist(pos.x, pos.y, playerCenter.x, playerCenter.y);
          boolean hit = false;
          //stroke(255);
          //rect(pos.x,pos.y,size,size);
          //line(center.x, center.y, playerCenter.x, playerCenter.y);
         
          //Collide with player
          if(shieldActive){
            if(distance < glowRadMax*activeScalar/2+size/2){
              hit = true;
            }
          } else if(shields > 0){
            if(distance < shieldGlow/2+size/2){
              hit = true;
            }
          } else if(distance < playerSize/2+size/2){
            hit = true;
          }
          
          if(hit){
            Collected(pos);
            ResetPos();
            //ScoreCount
            score += scoreTimeFactor*10*(1+((speed-startSpeed)*scoreSpeedFactor));
          }
          /*if(distance < playerSize/2+size/2){
            Collected(pos);
            ResetPos();
            //ScoreCount
            score += scoreTimeFactor*10*(1+((speed-startSpeed)*scoreSpeedFactor));
          }*/
          
          fill(c);
          noStroke();
          ellipse(pos.x, pos.y, size, size);
        }
    }
  }

}
public void mousePressed(){
  if(startMenu){
    if(inCircle(mid.x, mid.y, startButtonSize.x)){
      startMenu = false;
      game = true;
      Reset();
    }
    else if(inCircle(scaleFactor/6+RotateIcon.width/2, height-height/6+RotateIcon.width/2, RotateIcon.width)){
      RollOrRotate = true;
    }
    else if(inCircle(width-scaleFactor/6-RotateIcon.width/2, height-height/6+RotateIcon.width/2, RotateIcon.width)){
      RollOrRotate = false;
    }
    else if(inCircle(mid.x, mid.y*0.15f, ClearIcon.width*2)){
      SetBest(0);
      best = GetBest();
    }
  }
  else if(endMenu){
    if(inCircle(mid.x, mid.y, startButtonSize.x)){
      endMenu = false;
      game = true;
      Reset();
    }
    else if(inCircle(scaleFactor/20+HomeIcon.width/2, scaleFactor/20+HomeIcon.width/2, HomeIcon.width*2)){
      endMenu = false;
      startMenu = true;
    }
    else if(inCircle(width-SettingsIcon.width/2-scaleFactor/20, scaleFactor/20+SettingsIcon.width/2, SettingsIcon.width*2)){
      endMenu = false;
      settingsMenu = true;
    }
  }
  else if(settingsMenu){
    if(inCircle(mid.x-scaleFactor/6-RotateIcon.width/2, mid.y, RotateIcon.width)){
      RollOrRotate = true;
    }
    else if(inCircle(mid.x+scaleFactor/6+RotateIcon.width/2, mid.y, RotateIcon.width)){
      RollOrRotate = false;
    }
    else if(inCircle(scaleFactor/20+LeftIcon.width/2, scaleFactor/20+LeftIcon.width/2, LeftIcon.width*2)){
      settingsMenu = false;
      endMenu = true;
    }
  }
  else if(game){
    if(shields > 2){
      shields = 0;
      shieldActive = true;
      shieldActiveStart = millis();
    }
  }
}
public void mouseReleased(){
  if(game){
    shieldActive = false;
  }
}

//PC controlls
/*void keyPressed() {
  
  if (key == CODED) {
    if (keyCode == LEFT) {
      keyPress = true;
      tilt += keyMult*90;
    } else if (keyCode == RIGHT) {
      keyPress = true;
      tilt -= keyMult*90;
    } 
  }
  
  if(tilt > 90)
    tilt = 90;
  else if(tilt < -90)
    tilt = -90;
}
void keyReleased(){
  keyPress = false;
}*/

//Android controlls
class AccelerometerListener implements SensorEventListener {
  public void onSensorChanged(SensorEvent event) {
    
    if(RollOrRotate)
      az = event.values[1];
    else
      az = event.values[2];
    
    if(az > 5)
      az = 5;
    else if(az < -5)
      az = -5;
      
    tilt += az/5*90;
    
    if(tilt > 90)
      tilt = 90;
    else if(tilt < -90)
      tilt = -90;
  }
  public void onAccuracyChanged(Sensor sensor, int accuracy) {
  }
}
public void DBInit(){
  
String CREATE_DB_SQL = "CREATE TABLE data ( _id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, score INTEGER NOT NULL DEFAULT '0');";
String DROP_TABLE = "DROP TABLE data;";
String CLEAR_TABLE = "DELETE FROM data;";
  
db = new KetaiSQLite(this);  // open database file

  if ( db.connect() )
  {
    //Delete table CAREFUL!
    //db.execute(DROP_TABLE);
    
    // for initial app launch there are no tables so we make one
    if (!db.tableExists("data")){
      db.execute(CREATE_DB_SQL);
      //println("fresh DB");
    }
    
    //Clear table
    //db.execute(CLEAR_TABLE);
  }
}

public boolean CreateUser(String name){
  return db.execute("INSERT into data (`name`,`score`) VALUES ('"+name+"', 0);");
}

public String[] GetScoreboard(){
  
  ArrayList<String> results = new ArrayList<String>();
  int count = 3;
  
  db.query("SELECT score FROM data ORDER BY score DESC;");
  
  while (db.next () && count-- > 0){
    results.add(db.getString("score"));
  }
  
  return results.toArray(new String[0]);
}

public int GetBest(){
  db.query("SELECT score FROM data WHERE name='record';");
  
  if (db.next ())
    return db.getInt("score");
    
  return 0;
}

public void SetBest(int best){
  db.execute("UPDATE data SET score="+best+" WHERE name='record';");
}
class Particle {
  int c;
  PVector pos;
  String text;
  String type;
  PVector dirct;
  float angle;
  float spd;
  float sz;
  float life;
  float txtSize;
  float decay;
  float txtSizeMult = 1.025f;
  float alp = 255;
  String mode;
  
  Particle(String ty, PVector p, float si, float a, float s, float l, int cl){
    type = ty;
    pos = p;
    sz = si;
    angle = a;
    spd = s;
    life = l;
    c = cl;
    
    decay = si/life;
    float dirX = radians(angle-90);
    dirct = new PVector(cos(dirX), sin(dirX));
  }
  
  Particle(String ty, PVector p, float a, float s, float l, String t, float ts, int tc, String m){
    type = ty;
    pos = p;
    angle = a;
    spd = s;
    life = l;
    text = t;
    txtSize = ts;
    c = tc;
    mode = m;
    
    decay = alp/life;
    float dirX = radians(angle-90);
    dirct = new PVector(cos(dirX), sin(dirX));
  }
  
  public void Draw(){
    
    pos.add(dirct.x*spd, dirct.y*spd);
    
    if(type.equals("text")){
      textAlign(CENTER, CENTER);
      fill(c, alp);
      textSize(txtSize);
      text(text, pos.x, pos.y);
      alp -= decay;
      if(mode.equals("alphasize"))
        txtSize *= txtSizeMult;    
    }
    else if(type.equals("ball")){
      noStroke();
      fill(c);
      ellipse(pos.x, pos.y, sz, sz);
      sz -= decay;
    }
    
    life --;
  }
}

public void ShieldGained(PVector ps){
  for(int i = 0; i < particleDensity; i++){
    Particle p = new Particle("ball", new PVector(ps.x, ps.y), (playerSize/3)*2, random(360), 2, 25, shieldColor);
    particles.add(p);
  }
  //shieldSound.stop();
  //shieldSound = new SoundFile(this, "Shield.wav");
  //shieldSound.play();
}

public void Collected(PVector ps){
    for(int i = 0; i < particleDensity; i++){
      Particle p = new Particle("ball", new PVector(ps.x, ps.y), (playerSize/3)*2, random(360), 2, 25, collectColor);
      particles.add(p);
    }
    //collectSound.stop();
    //collectSound = new SoundFile(this, "Collect.wav");
    //collectSound.play();
}

public void Collided(PVector ps){
    for(int i = 0; i < particleDensity; i++){
      Particle p = new Particle("ball", new PVector(ps.x, ps.y), (playerSize/3)*2, random(360), 4, 25, obstacleColor);
      particles.add(p);
    }
}

public void ShieldExpired(PVector ps){
    for(int i = 0; i < particleDensity; i++){
      Particle p = new Particle("ball", new PVector(ps.x, ps.y), (playerSize/3)*2, 360/particleDensity*(i+1), 4, 15, shieldColor);
      particles.add(p);
    }
}

public void ShieldGlow(){
  if(glow){
    if(shieldGlow < glowRadMax)
      shieldGlow *= 1+glowScalar;
    else
      glow = false;
  }
  else{
    if(shieldGlow > glowRad)
      shieldGlow *= 1-glowScalar;
    else
      glow = true;
  }
  ellipse(mid.x, mid.y, shieldGlow, shieldGlow);
}

public void ShieldActive(){
  int deltaTime = millis()-shieldActiveStart;
  if(deltaTime <= shieldActiveDur*1000)
    ellipse(mid.x, mid.y, glowRadMax*activeScalar, glowRadMax*activeScalar);
  else{
    shieldActive = false;
  }
}

public void RecordAnimation(){
    //scaleFactor/5;
    //mid.x, mid.y*1.75
    PVector ps;
    for (int j = 0; j < 20; j++){
      ps = new PVector(random(mid.x-scaleFactor/3, mid.x+scaleFactor/3), random(mid.y*1.75f-scaleFactor/6, mid.y*1.75f+scaleFactor/6));
      for(int i = 0; i < particleDensity; i++){
        Particle p = new Particle("ball", new PVector(ps.x, ps.y), (playerSize/3)*2, random(360), 3, random(5, 100), collectColor);
        particles.add(p);
      }
    }
    //recordSound.play();
}
public void GameOver(){
  //bgSound.stop();
  //overSound.play();
  best = GetBest();
  particles = new ArrayList<Particle>();
  if(score > best){
    SetBest(PApplet.parseInt(score));
    RecordAnimation();
    best = GetBest();
  }
  
  game = false;
  endMenu = true;
}

public void Reset() {

  obstacles = new ArrayList<Obstacle>();
  collects = new ArrayList<Collect>();
  powers = new ArrayList<Power>();
  particles = new ArrayList<Particle>();
  b = false;

  dir = new PVector(0, 1);
  tilt = 0;
  score = 0;
  speed = startSpeed;

  //Obstacles
  for (int i = 0; i < obstaclesPerScr; i++) {
    Obstacle temp = new Obstacle();
    temp.size = playerSize/3;
    temp.c = obstacleColor;
    obstacles.add(temp);

    //Collects
    if (i < collectsPerScr) {
      Collect tempC = new Collect();
      tempC.size = (playerSize/3)*2;
      tempC.c = collectColor;
      tempC.speedMult = collectSpeedMult;
      collects.add(tempC);
    }

    //Shields
    if (i < shieldsPerScr) {
      Power tempP = new Power();
      tempP.type = "shield";
      tempP.size = (playerSize/3)*2;
      tempP.c = shieldColor;
      tempP.speedMult = shieldSpeedMult;
      powers.add(tempP);
    }
  }
  //Start particle
  Particle p = new Particle("text", new PVector(mid.x, mid.y), 0, 1, 25, "GO", scaleFactor/2, notifyColor, "alphasize");
  particles.add(p);
  
  //Sound
  //bgSound = new SoundFile(this, "GameBg.mp3");
  //bgSound.loop();
}
class Obstacle{    
  
  PVector pos;
  float speedMult = 1;
  int c;
  float size;
  
  Obstacle (){
    ResetPos();
  }
  
  public void Move (PVector posIn){
    pos.add(new PVector(posIn.x*speedMult, posIn.y*speedMult));
  }
  
  public void Draw (){
    if (pos.y-size/2 >= height){
      ResetPos();
    }
    else{
      if(pos.x+size/2 > 0 && pos.x-size/2 < width)
        if(pos.y+size/2 > 0 && pos.y-size/2 < height){
          
          float distance = dist(pos.x, pos.y, playerCenter.x, playerCenter.y);
         
          //stroke(255);
          //rect(pos.x,pos.y,size,size);
          //line(center.x, center.y, playerCenter.x, playerCenter.y);
          
          //Collide with player
          if(shieldActive){
            if(distance < glowRadMax*activeScalar/2+size/2){
              Collided(pos);
              ResetPos();
            }
          } else if(shields > 0){
            if(distance < shieldGlow/2+size/2){
              Collided(pos);
              shields --;
              if(shields == 0)
                ShieldExpired(mid);
              ResetPos();
            }
          } else {
            if(distance < playerSize/2+size/2){
              GameOver();
            }
          }
          /*if(distance < playerSize/2+size/2){
            Collided(pos);
            if(!shieldActive){
              if(shields > 0){
                shields --;
                if(shields == 0)
                  ShieldExpired(mid);
              }
              else{
                GameOver();
              }
            }
            ResetPos();
          }*/
          
          fill(c);
          noStroke();
          ellipse(pos.x, pos.y, size, size);
        }
    }
  }
  
  public void ResetPos(){
    PVector rand = new PVector(random(-width, width*2), random(-height, 0));
    pos = rand;
  }

} 
class Power extends Obstacle{
  
  String type;
  
  Power (){
    super();
  }

  public void DrawP (){
    if (pos.y > height){
      ResetPos();
    }
    else{ 
      //On screens
      if(pos.x+size/2 >= 0 && pos.x <= width)
        if(pos.y+size/2 >= 0 && pos.y <= height){
          
          PVector center = new PVector(pos.x+size/2, pos.y+size/2);
          float distance = dist(center.x, center.y, playerCenter.x, playerCenter.y);
          boolean hit = false;
          //stroke(255);
          //rect(pos.x,pos.y,size,size);
          //line(center.x, center.y, playerCenter.x, playerCenter.y);
         
          //Collide with player
          if(shieldActive){
            if(distance < glowRadMax*activeScalar/2+size/2){
              hit = true;
            }
          } else if(shields > 0){
            if(distance < shieldGlow/2+size/2){
              hit = true;
            }
          } else if(distance < playerSize/2+size/2){
            hit = true;
          }
          
          if(hit){
            ResetPos();
            //Power
            if(type.equals("shield")){
              ShieldGained(pos);
              shields ++;
            }
          }
          /*if(distance < playerSize/2+size/2){
            ResetPos();
            //Power
            if(type.equals("shield")){
              ShieldGained(pos);
              shields ++;
            }
          }*/
          
          fill(c);
          noStroke();
          ellipse(pos.x+size/2, pos.y+size/2, size, size);
        }
    }
  }
}
  public void settings() {  fullScreen();  smooth(); }
}