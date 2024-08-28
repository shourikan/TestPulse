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
import android.view.WindowManager; 
import android.os.Bundle; 
import android.content.Intent; 
import ketai.net.bluetooth.*; 
import ketai.ui.*; 
import ketai.net.*; 
import oscP5.*; 

import java.util.HashMap; 
import java.util.ArrayList; 
import java.io.File; 
import java.io.BufferedReader; 
import java.io.PrintWriter; 
import java.io.InputStream; 
import java.io.OutputStream; 
import java.io.IOException; 

public class TestPulse extends PApplet {


// Game

// import cassette.audiofiles.SoundFile;








//  Bluetooth

    

 



// Sensors
SensorManager manager;
Sensor sensor;
AccelerometerListener listener;

// Bluetooth
KetaiBluetooth bt;
KetaiList connectionList;
PVector multiplayerPos  = new PVector();
PVector multiplayerPosTemp  = new PVector();
PVector relativePos = new PVector();
PVector realPos = new PVector();
boolean paired = false;
boolean triggerPlay = false;
boolean ready = false;
boolean winner = true;

// Game
Context context;
// SoundFile collectSound, shieldSound, overSound, bgSound, slowSound, recordSound;
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
float tiltDecay = 0.95f;
PVector mid;
PVector dir;
// PShape playerCharacter;
// PShape player2Character;

int collectColor = color(255, 177, 54);
int obstacleColor = color(173, 73, 254);
int interfaceColor = color(0, 237, 126);
int notifyColor = color(255, 0, 115);
int shieldColor = color(2, 162, 255);
int playerColor = color(0, 237, 126);
int multiplayerColor = color(255, 255, 255);
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
float svgSize = 15;
float startSpeed = 4;
float speed;
float speedMult = 0.075f;
float speedLimit = 20;
float speedLimitInc = 7;
float collectSpeedMult = 0.8f;
float shieldSpeedMult = 0.6f;
int fps = 60;
float scaleFactor;
PVector screenFactor;
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
float lastFrame = 0;
float dT = 1;

public void setup() {
	
	// size(1080,1920);
	orientation(PORTRAIT);
	
	//frameRate(fps);
	background(bgColor);
	
	noStroke();
	fill(255);
	// randomSeed(int(random(100)));
	// randomSeed(1);
	// Bluetooth
	bt.start();
	
	// DB
	DBInit();
	best = GetBest();
	// SetBest(0);
	CreateUser("record");
	// println(GetBest());
	
	// Sounds
	// overSound = new SoundFile(this, "Over.mp3");
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
	// player2Character = loadShape("Player2.svg");
	// playerCharacter = loadShape("Player.svg");
	
	//println(width+" "+height);
	//scaleFactor = min(width, height);
	scaleFactor = 1080;
	screenFactor = new PVector(PApplet.parseFloat(width) / 1080, PApplet.parseFloat(height) / 1920);
	//println("SF : " + screenFactor);
	
	lastFrame = millis();
	
	startButtonSize = new PVector(scaleFactor / 2,(scaleFactor / 2) / 5);
	
	mid = new PVector(width / 2, height / 2);
	//speed /= fps/60;
	
	//Android Sensors
	context = getActivity();
	manager = (SensorManager)context.getSystemService(Context.SENSOR_SERVICE);
	sensor = manager.getDefaultSensor(Sensor.TYPE_GYROSCOPE);
	listener = new AccelerometerListener();
	manager.registerListener(listener, sensor, SensorManager.SENSOR_DELAY_GAME);
	
	//Player
	playerCenter = mid;
	playerSize = scaleFactor / 20 * ((screenFactor.x + screenFactor.y) / 2);
	// playerCharacter.scale(playerSize / svgSize);
	// player2Character.scale(playerSize / svgSize);
	
	//Multiplayer
	multiplayerPos = new PVector(width / 2, height / 2);
	multiplayerPosTemp = new PVector(width / 2, height / 2);
	
	shapeMode(CENTER);
	
	//Effects
	shieldGlow = playerSize;
	glowRad = playerSize * 1.05f;
	glowRadMax = playerSize * 1.3f;
	
	//println(SettingsIcon.width, SettingsIcon.height);
}

public void draw() {
	
	float time = millis();
	dT = (time - lastFrame) / (1000.0f / 60.0f);
	//dT = (1 - abs(dT) > 0.5) ? dT : 1.0;
	dT = 1.0f;
	lastFrame = time;
	
	if (startMenu) {
		//Score
		background(bgColor);
		textSize(scaleFactor / 10);
		textAlign(CENTER, CENTER);
		fill(collectColor);
		text("TOP : " + round(best), mid.x, mid.y * 0.35f);
		image(ClearIcon, mid.x - ClearIcon.width / 2, mid.y * 0.15f - ClearIcon.width / 2);
		
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
		textSize(scaleFactor / 15);
		fill(255);
		textAlign(CENTER, CENTER);
		if (paired && ready)
			text("WAITING...", mid.x, mid.y);
		else
			text("PLAY", mid.x, mid.y);
		
		//Controls
		image(RollIcon, scaleFactor / 6, height - height / 6);
		image(RotateIcon, width - scaleFactor / 6 - RotateIcon.width, height - height / 6);
		strokeWeight(3);
		stroke(interfaceColor);
		noFill();
		if (RollOrRotate)
			ellipse(scaleFactor / 6 + RotateIcon.width / 2, height - height / 6 + RotateIcon.width / 2, RotateIcon.width, RotateIcon.width);
		else
			ellipse(width - scaleFactor / 6 - RotateIcon.width / 2, height - height / 6 + RotateIcon.width / 2, RotateIcon.width, RotateIcon.width);
		
		//Multiplayer
		if (bt.getConnectedDeviceNames().size() <= 0) {
			paired = false;
			stroke(multiplayerColor);
		}
		else{
			paired = true;
		}
		image(BluetoothIcon, width / 2 - BluetoothIcon.width / 2, height - height / 6 - BluetoothIcon.width);
		ellipse(width / 2, height - height / 6 - BluetoothIcon.width / 2, BluetoothIcon.width, BluetoothIcon.width);
	}
	else if (endMenu) {
		background(bgColor);
		stroke(interfaceColor);
		noFill();
		ellipse(mid.x, mid.y, startButtonSize.x, startButtonSize.x);
		textSize(scaleFactor / 15);
		fill(255);
		textAlign(CENTER, CENTER);
		if (paired && ready)
			text("WAITING...", mid.x, mid.y);
		else if (paired)
			text("GO AGAIN!", mid.x, mid.y);
		else
			text("TRY AGAIN!", mid.x, mid.y);
		textSize(scaleFactor / 10);
		if (paired) {
			if (winner) {
				fill(collectColor);
				text("WINNER!", mid.x, mid.y * 0.35f);
			} else {
				fill(obstacleColor);
				text("LOSER!", mid.x, mid.y * 0.35f);
			}
		}
		else{
			fill(obstacleColor);
			text("GAME OVER", mid.x, mid.y * 0.35f);
		}
		fill(collectColor);
		textSize(scaleFactor / 5);
		text(round(score), mid.x, mid.y * 1.75f);
		image(SettingsIcon, width - SettingsIcon.width - scaleFactor / 20, scaleFactor / 20);
		image(HomeIcon, scaleFactor / 20, scaleFactor / 20);
		
		//Particles
		for (int i = 0; i < particles.size(); i++) {
			particles.get(i).Draw();
			if (particles.get(i).life <= 0)
				particles.remove(i);
		}
	}
	else if (settingsMenu) {
		background(bgColor);
		image(RollIcon, mid.x - RollIcon.width - scaleFactor / 6, mid.y - RollIcon.width / 2);
		image(RotateIcon, mid.x + scaleFactor / 6, mid.y - RollIcon.width / 2);
		image(LeftIcon, scaleFactor / 20, scaleFactor / 20);
		strokeWeight(3);
		stroke(interfaceColor);
		noFill();
		if (RollOrRotate)
			ellipse(mid.x - scaleFactor / 6 - RotateIcon.width / 2, mid.y, RotateIcon.width, RotateIcon.width);
		else
			ellipse(mid.x + scaleFactor / 6 + RotateIcon.width / 2, mid.y, RotateIcon.width, RotateIcon.width);
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
		
		multiplayerPos = new PVector(multiplayerPosTemp.x, multiplayerPosTemp.y);
		
		//Score
		score += ((1.0f / 60.0f) * scoreTimeFactor * (1 + ((speed - startSpeed) * scoreSpeedFactor))) * dT;
		if (score > best && !b) {
			RecordAnimation();
			b = true;
		}
		
		//Speed limit
		if (speed > speedLimit && !slowing) {
			slowing = true;
			//slowSound.play();
		}
		
		//Speed mult
		if (slowing && speed > startSpeed) {
			speed *= 1 - (slowingMult / 60);
		}
		else{
			if (slowing) {
				slowing = false;
				speedLimit += speedLimitInc;
			}
			speed *= 1 + (speedMult / 60);
		}
		
		//Tilt decay
		if (!keyPress)
			tilt *= tiltDecay;
		
		//Direction, Movement step
		float dirX = (RollOrRotate) ? radians(tilt * (1) + 90) : radians(tilt * (- 1) + 90);
		dir = new PVector(cos(dirX), sin(dirX));
		PVector mov = new PVector(dir.x * speed * dT * screenFactor.x, dir.y * speed * dT * screenFactor.y);
		realPos.add(new PVector(mov.x * (- 1),(dir.y - 1) * - 1 * speed * dT * screenFactor.y));
		relativePos.add(new PVector(mov.x,(dir.y - 1) * speed * dT * screenFactor.y));
		//realPos.add(new PVector(mov.x*(-1), 0));
		
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
			if (particles.get(i).life <= 0)
				particles.remove(i);
		}
		
		if (bt.getConnectedDeviceNames().size() > 0 && game)
			sendData();
		
		//Shield
		fill(shieldColor);
		noStroke();
		if (shieldActive) {
			ShieldActive();
			
			textAlign(CENTER, CENTER);
			textSize(scaleFactor / 15);
			text(round(shieldActiveDur - (millis() - shieldActiveStart) / 1000), mid.x,(mid.y + playerSize / 2) + (scaleFactor / 10 / 2));
		}
		else if (shields > 0) {
			ShieldGlow();
			
			if (shields > 2) {
				textAlign(CENTER, CENTER);
				textSize(scaleFactor / 15);
				text("!", mid.x,(mid.y + playerSize / 2) + (scaleFactor / 10 / 2));
			}
		}
		
		//Multiplayer character indicator (off-screen)
		float x_ = multiplayerPos.x + relativePos.x;
		float y_ = multiplayerPos.y + relativePos.y;
		//Left
		if (x_ < 0) {
			//left top
			if (y_ < 0) {
				Triangle(new PVector(playerSize, playerSize), playerSize, "lefttop");
			}
			//left bot
			else if (y_ > height) {
				Triangle(new PVector(playerSize, height - playerSize), playerSize, "leftbot");
			}
			//left
			else if (x_ + playerSize / 2 < 0) {
				Triangle(new PVector(playerSize, y_), playerSize, "left");
			}
		}
		//Right
		else if (x_ > width) {
			//right top
			if (y_ < 0) {
				Triangle(new PVector(width - playerSize, playerSize), playerSize, "righttop");
			}
			//right bot
			else if (y_ > height) {
				Triangle(new PVector(width - playerSize, height - playerSize), playerSize, "rightbot");
			}
			//right
			else if (x_ - playerSize / 2 > width) {
				Triangle(new PVector(width - playerSize, y_), playerSize, "right");
			}
		}
		else{
			//top
			if (y_ + playerSize / 2 < 0) {
				Triangle(new PVector(x_, playerSize), playerSize, "top");
			}
			//bot
			else if (y_ - playerSize / 2 > height) {
				Triangle(new PVector(x_, height - playerSize), playerSize, "bot");
			}
			else{
				fill(multiplayerColor);
				noStroke();
				ellipse(multiplayerPos.x + relativePos.x, multiplayerPos.y + relativePos.y, playerSize, playerSize);
				// shape(player2Character, multiplayerPos.x + relativePos.x, multiplayerPos.y + relativePos.y);
			}
		}
		
		// Player character
		fill(playerColor);
		noStroke();
		ellipse(mid.x, mid.y, playerSize, playerSize);
		// shape(playerCharacter, mid.x, mid.y);
		
		// Score
		textAlign(CENTER, CENTER);
		textSize(scaleFactor / 7);
		fill(collectColor);
		text(round(score), mid.x, height - scaleFactor / 7);
		// text(frameRate, mid.x, height - scaleFactor / 7);
		
		//Info text
		textAlign(LEFT, TOP);
		textSize(scaleFactor / 20);
		fill(255);
		text(round(frameRate), 10, 50);
		text("dT : " + dT, 10, 100);
		text("SF" + screenFactor, 10, 150);
		// text(System.nanoTime(), 10, 150);
		// text(width + " " + height, 10, 200);
		
		//Speed bar
		float yP = width * ((speed - startSpeed) / (speedLimit - startSpeed));
		stroke(255);
		strokeWeight(1);
		line(0, height - 3, yP, height - 3);
		// strokeWeight(1);
		// fill(interfaceColor);
		// ellipse(scaleFactor/200, yP, scaleFactor/50, scaleFactor/50);
		
		//Score bar
		if (score <= best) {
			yP = width * (score / best); 
			stroke(collectColor);
			strokeWeight(1);
			line(0, height - 1, yP, height - 1);
			// strokeWeight(1);
			// fill(interfaceColor);
			// ellipse(yP, height-scaleFactor/200, scaleFactor/50, scaleFactor/50);
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
public void onCreate(Bundle savedInstanceState) {
	super.onCreate(savedInstanceState);
	bt = new KetaiBluetooth(this);
	getActivity().getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
}

public void onActivityResult(int requestCode, int resultCode, Intent data) {
	bt.onActivityResult(requestCode, resultCode, data);
}

public void bluetoothClose() {
	if (bt.getConnectedDeviceNames().size()>0) {
		bt.disconnectDevice(bt.getConnectedDeviceNames().get(0));
		ready = false;
	}
	paired = false;
	GameOver();
}

public void deviceList() {
	if (bt.getDiscoveredDeviceNames().size() > 0)
		connectionList = new KetaiList(this, bt.getDiscoveredDeviceNames());
	else if (bt.getPairedDeviceNames().size() > 0)
		connectionList = new KetaiList(this, bt.getPairedDeviceNames());
}

//Receive
public void onBluetoothDataEvent(String who, byte[] data)
{
	KetaiOSCMessage m = new KetaiOSCMessage(data);
	if (m.isValid())
	 {
		if (m.checkAddrPattern(" / remoteMouse / "))
		{
			if (m.checkTypetag("iiii"))
			{
				multiplayerPosTemp.x = map(m.get(0).intValue(), 0, m.get(2).intValue(), 0, width);
				multiplayerPosTemp.y = map(m.get(1).intValue(), 0, m.get(3).intValue(), 0, height);
			}
		}
		else if (m.checkAddrPattern(" / remotePlay / "))
		{
			if (m.checkTypetag("ii"))
			{
				if (m.get(0).intValue() == 9) {
					triggerPlay = true;
					randomSeed(m.get(1).intValue());
					if (ready)
						Start();
				}
			}
		}
		else if (m.checkAddrPattern(" / remoteDied / "))
		{
			if (m.checkTypetag("i"))
			{
				if (m.get(0).intValue() == 5) {
					println("enemy died");
					if (game)
						GameOver();
				}
			}
		}
	}
}

//Send
public void sendData() {
	OscMessage m = new OscMessage(" / remoteMouse / ");
	m.add(PApplet.parseInt(realPos.x));
	m.add(PApplet.parseInt(realPos.y));
	m.add(width);
	m.add(height);
	
	bt.broadcast(m.getBytes());
}

//Play
public void sendPlay() {
	OscMessage m = new OscMessage(" / remotePlay / ");
	int play_ = 9;
	int seed = PApplet.parseInt(random(1000));
	randomSeed(seed);
	m.add(play_);
	m.add(seed);
	
	bt.broadcast(m.getBytes());
}

//GameOver
public void sendDied() {
	OscMessage m = new OscMessage(" / remoteDied / ");
	int died_ = 5;
	m.add(died_);
	println("sending died");
	bt.broadcast(m.getBytes());
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
	
	Collect() {
		size = (playerSize / 3) * 2;
		c = collectColor;
		speedMult = collectSpeedMult;
		shape = loadShape("Collect.svg");
		shapeMode(CENTER);
		shape.scale(size / svgSize);
		ResetPos();
	}
	
	public void DrawC() {
		if (pos.y - size / 2 >= height) {
			ResetPos();
		}
		else{ 
			//On screens
			if (pos.x + size / 2 > 0 && pos.x - size / 2 < width)
				if (pos.y + size / 2 > 0 && pos.y - size / 2 < height) {
					
					float distance = dist(pos.x, pos.y, playerCenter.x, playerCenter.y);
					boolean hit = false;
				//stroke(255);
				//rect(pos.x,pos.y,size,size);
				//line(center.x, center.y, playerCenter.x, playerCenter.y);
				
				//Collide with player
				if (shieldActive) {
					if (distance < glowRadMax * activeScalar / 2 + size / 2) {
						hit = true;
					}
				} else if (shields > 0) {
					if (distance < shieldGlow / 2 + size / 2) {
						hit = true;
					}
				} else if (distance < playerSize / 2 + size / 2) {
					hit = true;
				}
				
				if (hit) {
					Collected(pos);
					ResetPos();
					//ScoreCount
					score += scoreTimeFactor * 10 * (1 + ((speed - startSpeed) * scoreSpeedFactor));
				}
				/*if(distance < playerSize/2+size/2){
				Collected(pos);
				ResetPos();
				//ScoreCount
				score += scoreTimeFactor*10*(1+((speed-startSpeed)*scoreSpeedFactor));
			}*/
				
				fill(c);
				noStroke();
				//ellipse(pos.x, pos.y, size, size);
				shape(shape, pos.x, pos.y);
			}
		}
	}
	
}
public void mousePressed(){
  if(startMenu){
    //Play
    if(inCircle(mid.x, mid.y, startButtonSize.x)){
      Start();
    }
    //Rotate
    else if(inCircle(scaleFactor/6+RotateIcon.width/2, height-height/6+RotateIcon.width/2, RotateIcon.width)){
      RollOrRotate = true;
    }
    //Roll
    else if(inCircle(width-scaleFactor/6-RotateIcon.width/2, height-height/6+RotateIcon.width/2, RotateIcon.width)){
      RollOrRotate = false;
    }
    //Erase
    else if(inCircle(mid.x, mid.y*0.15f, ClearIcon.width*2)){
      SetBest(0);
      best = GetBest();
    }
    //Multiplayer
    else if(inCircle(width/2, height-height/6-RotateIcon.width/2, RotateIcon.width)){
      if(paired){
        bluetoothClose();
      }else {
        deviceList();
      }
      
    }
  }
  else if(endMenu){
    //Try again
    if(inCircle(mid.x, mid.y, startButtonSize.x)){
      Start();
    }
    //Home
    else if(inCircle(scaleFactor/20+HomeIcon.width/2, scaleFactor/20+HomeIcon.width/2, HomeIcon.width*2)){
      endMenu = false;
      startMenu = true;
    }
    //Settings
    else if(inCircle(width-SettingsIcon.width/2-scaleFactor/20, scaleFactor/20+SettingsIcon.width/2, SettingsIcon.width*2)){
      endMenu = false;
      settingsMenu = true;
    }
  }
  else if(settingsMenu){
    //Rotate
    if(inCircle(mid.x-scaleFactor/6-RotateIcon.width/2, mid.y, RotateIcon.width)){
      RollOrRotate = true;
    }
    //Roll
    else if(inCircle(mid.x+scaleFactor/6+RotateIcon.width/2, mid.y, RotateIcon.width)){
      RollOrRotate = false;
    }
    //Back
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

//BluetoothListDevices
public void onKetaiListSelection(KetaiList connectionList)
{
  String selection = connectionList.getSelection();
  bt.connectToDeviceByName(selection);
  connectionList = null;
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
	
	Particle(String ty, PVector p, float si, float a, float s, float l, int cl) {
		type = ty;
		pos = p;
		sz = si;
		angle = a;
		spd = s;
		life = l;
		c = cl;
		
		decay = si / life;
		float dirX = radians(angle - 90);
		dirct = new PVector(cos(dirX), sin(dirX));
	}
	
	Particle(String ty, PVector p, float a, float s, float l, String t, float ts, int tc, String m) {
		type = ty;
		pos = p;
		angle = a;
		spd = s;
		life = l;
		text = t;
		txtSize = ts;
		c = tc;
		mode = m;
		
		decay = alp / life;
		float dirX = radians(angle - 90);
		dirct = new PVector(cos(dirX), sin(dirX));
	}
	
	public void Draw() {
		
		pos.add(dirct.x * spd * dT, dirct.y * spd * dT);
		
		if (type.equals("text")) {
			textAlign(CENTER, CENTER);
			fill(c, alp);
			textSize(txtSize);
			text(text, pos.x, pos.y);
			alp -= decay;
			if (mode.equals("alphasize"))
				txtSize *= txtSizeMult;    
		}
		else if (type.equals("ball")) {
			noStroke();
			fill(c);
			ellipse(pos.x, pos.y, sz, sz);
			sz -= decay;
		}
		
		life --;
	}
}

public void ShieldGained(PVector ps) {
	for (int i = 0; i < particleDensity; i++) {
		Particle p = new Particle("ball", new PVector(ps.x, ps.y),(playerSize / 3) * 2, random(360), 2, 25, shieldColor);
		particles.add(p);
	}
	//shieldSound.stop();
	//shieldSound = new SoundFile(this, "Shield.wav");
	//shieldSound.play();
}

public void Collected(PVector ps) {
	for (int i = 0; i < particleDensity; i++) {
		Particle p = new Particle("ball", new PVector(ps.x, ps.y),(playerSize / 3) * 2, random(360), 2, 25, collectColor);
		particles.add(p);
	}
	//collectSound.stop();
	//collectSound = new SoundFile(this, "Collect.wav");
	//collectSound.play();
}

public void Collided(PVector ps) {
	for (int i = 0; i < particleDensity; i++) {
		Particle p = new Particle("ball", new PVector(ps.x, ps.y),(playerSize / 3) * 2, random(360), 4, 25, obstacleColor);
		particles.add(p);
	}
}

public void ShieldExpired(PVector ps) {
	for (int i = 0; i < particleDensity; i++) {
		Particle p = new Particle("ball", new PVector(ps.x, ps.y),(playerSize / 3) * 2, 360 / particleDensity * (i + 1), 4, 15, shieldColor);
		particles.add(p);
	}
}

public void ShieldGlow() {
	if (glow) {
		if (shieldGlow < glowRadMax)
			shieldGlow *= 1 + glowScalar;
		else
			glow = false;
	}
	else{
		if (shieldGlow > glowRad)
			shieldGlow *= 1 - glowScalar;
		else
			glow = true;
	}
	ellipse(mid.x, mid.y, shieldGlow, shieldGlow);
}

public void ShieldActive() {
	int deltaTime = millis() - shieldActiveStart;
	if (deltaTime <= shieldActiveDur * 1000)
		ellipse(mid.x, mid.y, glowRadMax * activeScalar, glowRadMax * activeScalar);
	else{
		shieldActive = false;
	}
}

public void RecordAnimation() {
	//scaleFactor/5;
	//mid.x, mid.y*1.75
	PVector ps;
	for (int j = 0; j < 20; j++) {
		ps = new PVector(random(mid.x - scaleFactor / 3, mid.x + scaleFactor / 3), random(mid.y * 1.75f - scaleFactor / 6, mid.y * 1.75f + scaleFactor / 6));
		for (int i = 0; i < particleDensity; i++) {
			Particle p = new Particle("ball", new PVector(ps.x, ps.y),(playerSize / 3) * 2, random(360), 3, random(5, 100), collectColor);
			particles.add(p);
		}
	}
	//recordSound.play();
}
public void GameOver() {
	//bgSound.stop();
	//overSound.play();
	best = GetBest();
	particles = new ArrayList<Particle>();
	if (score > best) {
		SetBest(PApplet.parseInt(score));
		RecordAnimation();
		best = GetBest();
	}
	
	game = false;
	endMenu = true;
}

public void Start() {
	if(!ready)
		Reset();
	if (paired && !ready) {
		sendPlay();
		ready = true;
	}
	if ((paired && triggerPlay) || !paired) {
		ready = false;
		triggerPlay = false;
		startMenu = false;
		endMenu = false;
		settingsMenu = false;
		game = true;
	}
}

public void Reset() {
	
	obstacles = new ArrayList<Obstacle>();
	collects = new ArrayList<Collect>();
	powers = new ArrayList<Power>();
	particles = new ArrayList<Particle>();
	b = false;
	winner = true;
	
	//Position
	realPos = new PVector(width / 2, height / 2);
	multiplayerPos = new PVector(width / 2, height / 2);
	multiplayerPosTemp = new PVector(width / 2, height / 2);
	relativePos = new PVector(0, 0);
	
	dir = new PVector(0, 1);
	tilt = 0;
	score = 0;
	speed = startSpeed;
	
	//Obstacles
	for (int i = 0; i < obstaclesPerScr; i++) {
		Obstacle temp = new Obstacle();
		obstacles.add(temp);
		
		//Collects
		if (i < collectsPerScr) {
			Collect tempC = new Collect();
			collects.add(tempC);
		}
		
		//Shields
		if (i < shieldsPerScr) {
			Power tempP = new Power();
			powers.add(tempP);
		}
	}
	//Start particle
	Particle p = new Particle("text", new PVector(mid.x, mid.y), 0, 1, 25, "GO", scaleFactor / 2, notifyColor, "alphasize");
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
	PShape shape;
	
	Obstacle() {
		size = playerSize / 3;
		c = obstacleColor;
		shape = loadShape("Obstacle.svg");
		shape.scale(size / svgSize);
		shapeMode(CENTER);
		ResetPos();
	}
	
	public void Move(PVector posIn) {
		pos.add(new PVector(posIn.x * speedMult, posIn.y * speedMult));
	}
	
	public void Draw() {
		if (pos.y - size / 2 >= height) {
			ResetPos();
		}
		else{
			if (pos.x + size / 2 > 0 && pos.x - size / 2 < width)
				if (pos.y + size / 2 > 0 && pos.y - size / 2 < height) {
					
					float distance = dist(pos.x, pos.y, playerCenter.x, playerCenter.y);
					
					//stroke(255);
					//rect(pos.x,pos.y,size,size);
					//line(center.x, center.y, playerCenter.x, playerCenter.y);
					
					//Collide with player
					if (shieldActive) {
						if (distance < glowRadMax * activeScalar / 2 + size / 2) {
							Collided(pos);
							ResetPos();
					}
				} else if (shields > 0) {
					if (distance < shieldGlow / 2 + size / 2) {
						Collided(pos);
						shields --;
						if (shields == 0)
							ShieldExpired(mid);
						ResetPos();
					}
				} else {
					if (distance < playerSize / 2 + size / 2) {
						winner = false;
						sendDied();
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
				//ellipse(pos.x, pos.y, size, size);
				shape(shape, pos.x, pos.y);
			}
		}
	}
	
	public void ResetPos() {
		PVector rand = new PVector(random(- width, width * 2), random(- height, 0));
		pos = rand;
	}
	
} 
class Power extends Obstacle{
	
	String type;
	
	Power() {
		type = "shield";
		size = (playerSize / 3) * 2;
		c = shieldColor;
		speedMult = shieldSpeedMult;
		shape = loadShape("Power_Shield.svg");
		shapeMode(CENTER);
		shape.scale(size / svgSize);
		ResetPos();
	}
	
	public void DrawP() {
		if (pos.y - size / 2 >= height) {
			ResetPos();
		}
		else{ 
			//On screens
			if (pos.x + size / 2 > 0 && pos.x - size / 2 < width)
				if (pos.y + size / 2 > 0 && pos.y - size / 2 < height) {
					
					float distance = dist(pos.x, pos.y, playerCenter.x, playerCenter.y);
					boolean hit = false;
				//stroke(255);
				//rect(pos.x,pos.y,size,size);
				//line(center.x, center.y, playerCenter.x, playerCenter.y);
				
				//Collide with player
				if (shieldActive) {
					if (distance < glowRadMax * activeScalar / 2 + size / 2) {
						hit = true;
					}
				} else if (shields > 0) {
					if (distance < shieldGlow / 2 + size / 2) {
						hit = true;
					}
				} else if (distance < playerSize / 2 + size / 2) {
					hit = true;
				}
				
				if (hit) {
					ResetPos();
					//Power
					if (type.equals("shield")) {
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
				//ellipse(pos.x + size / 2, pos.y + size / 2, size, size);
				shape(shape, pos.x, pos.y);
			}
		}
	}
}
public void Triangle(PVector pos, float size, String orientation) {
	fill(multiplayerColor);
	switch(orientation) {
		case "lefttop" : {
			triangle(pos.x - size / 2, pos.y - size / 2, pos.x + size / 2 + size / 4, pos.y - size / 4, pos.x - size / 4, pos.y + size / 2 + size / 4);
			break;
		}
		case "leftbot" : {
			triangle(pos.x - size / 2, pos.y + size / 2, pos.x + size / 2 + size / 4, pos.y + size / 4, pos.x - size / 4, pos.y - size / 2 - size / 4);
			break;
		}
		case "left" : {
			triangle(pos.x - size / 2, pos.y, pos.x + size / 2, pos.y - size / 2, pos.x + size / 2, pos.y + size / 2);
			break;
		}
		case "righttop" : {
			triangle(pos.x + size / 2, pos.y - size / 2, pos.x - size / 2 - size / 4, pos.y - size / 4, pos.x + size / 4, pos.y + size / 2 + size / 4);
			break;
		}
		case "rightbot" : {
			triangle(pos.x + size / 2, pos.y - size / 2, pos.x - size / 2 - size / 4, pos.y + size / 4, pos.x + size / 4, pos.y - size / 2 - size / 4);
			break;
		}
		case "right" : {
			triangle(pos.x + size / 2, pos.y, pos.x - size / 2, pos.y - size / 2, pos.x - size / 2, pos.y + size / 2);
			break;
		}
		case "top" : {
			triangle(pos.x, pos.y - size / 2, pos.x - size / 2, pos.y + size / 2, pos.x + size / 2, pos.y + size / 2);
			break;
		}
		case "bot" : {
			triangle(pos.x, pos.y + size / 2, pos.x - size / 2, pos.y - size / 2, pos.x + size / 2, pos.y - size / 2);
			break;  
		}
	}
}
  public void settings() { 	fullScreen(P2D); 	smooth(); }
  static public void main(String[] passedArgs) {
    String[] appletArgs = new String[] { "TestPulse" };
    if (passedArgs != null) {
      PApplet.main(concat(appletArgs, passedArgs));
    } else {
      PApplet.main(appletArgs);
    }
  }
}
