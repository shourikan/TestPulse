void onCreate(Bundle savedInstanceState) {
  super.onCreate(savedInstanceState);
  bt = new KetaiBluetooth(this);
}

void onActivityResult(int requestCode, int resultCode, Intent data) {
  bt.onActivityResult(requestCode, resultCode, data);
}

void bluetoothClose(){
  if(bt.getConnectedDeviceNames().size()>0){
    bt.disconnectDevice(bt.getConnectedDeviceNames().get(0));
    ready = false;
  }
  paired = false;
}

void deviceList(){
  if (bt.getDiscoveredDeviceNames().size() > 0)
    connectionList = new KetaiList(this, bt.getDiscoveredDeviceNames());
  else if (bt.getPairedDeviceNames().size() > 0)
    connectionList = new KetaiList(this, bt.getPairedDeviceNames());
}

//Receive
void onBluetoothDataEvent(String who, byte[] data)
{
  KetaiOSCMessage m = new KetaiOSCMessage(data);
  if (m.isValid())
  {
    if (m.checkAddrPattern("/remoteMouse/"))
    {
      if (m.checkTypetag("iiii"))
      {
        multiplayerPos.x = map(m.get(0).intValue(), 0, m.get(2).intValue(), 0, width);
        multiplayerPos.y = map(m.get(1).intValue(), 0, m.get(3).intValue(), 0, height);
      }
    }
    else if (m.checkAddrPattern("/remotePlay/"))
    {
      if (m.checkTypetag("i"))
      {
        if(m.get(0).intValue()==9){
          triggerPlay = true;
          if (ready)
            Start();
        }
      }
    }
    else if (m.checkAddrPattern("/remoteDied/"))
    {
      if (m.checkTypetag("i"))
      {
        if(m.get(0).intValue()==5){
          println("enemy died");
          if (paired && game)
            GameOver();
        }
      }
    }
  }
}

//Send
void sendData(){
  OscMessage m = new OscMessage("/remoteMouse/");
  m.add(int(realPos.x));
  m.add(int(realPos.y));
  m.add(width);
  m.add(height);
  
  bt.broadcast(m.getBytes());
}

//Play
void sendPlay(){
  OscMessage m = new OscMessage("/remotePlay/");
  int play_ = 9;
  m.add(play_);
  
  bt.broadcast(m.getBytes());
}

//GameOver
void sendDied(){
  OscMessage m = new OscMessage("/remoteDied/");
  int died_ = 5;
  m.add(died_);
  println("sending died");
  bt.broadcast(m.getBytes());
}