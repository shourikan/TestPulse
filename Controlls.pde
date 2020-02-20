void mousePressed(){
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
    else if(inCircle(mid.x, mid.y*0.15, ClearIcon.width*2)){
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
void mouseReleased(){
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
void onKetaiListSelection(KetaiList connectionList)
{
  String selection = connectionList.getSelection();
  bt.connectToDeviceByName(selection);
  connectionList = null;
}