class Particle {
  color c;
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
  float txtSizeMult = 1.025;
  float alp = 255;
  String mode;
  
  Particle(String ty, PVector p, float si, float a, float s, float l, color cl){
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
  
  Particle(String ty, PVector p, float a, float s, float l, String t, float ts, color tc, String m){
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
  
  void Draw(){
    
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

void ShieldGained(PVector ps){
  for(int i = 0; i < particleDensity; i++){
    Particle p = new Particle("ball", new PVector(ps.x, ps.y), (playerSize/3)*2, random(360), 2, 25, shieldColor);
    particles.add(p);
  }
  //shieldSound.stop();
  //shieldSound = new SoundFile(this, "Shield.wav");
  //shieldSound.play();
}

void Collected(PVector ps){
    for(int i = 0; i < particleDensity; i++){
      Particle p = new Particle("ball", new PVector(ps.x, ps.y), (playerSize/3)*2, random(360), 2, 25, collectColor);
      particles.add(p);
    }
    //collectSound.stop();
    //collectSound = new SoundFile(this, "Collect.wav");
    //collectSound.play();
}

void Collided(PVector ps){
    for(int i = 0; i < particleDensity; i++){
      Particle p = new Particle("ball", new PVector(ps.x, ps.y), (playerSize/3)*2, random(360), 4, 25, obstacleColor);
      particles.add(p);
    }
}

void ShieldExpired(PVector ps){
    for(int i = 0; i < particleDensity; i++){
      Particle p = new Particle("ball", new PVector(ps.x, ps.y), (playerSize/3)*2, 360/particleDensity*(i+1), 4, 15, shieldColor);
      particles.add(p);
    }
}

void ShieldGlow(){
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

void ShieldActive(){
  int deltaTime = millis()-shieldActiveStart;
  if(deltaTime <= shieldActiveDur*1000)
    ellipse(mid.x, mid.y, glowRadMax*activeScalar, glowRadMax*activeScalar);
  else{
    shieldActive = false;
  }
}

void RecordAnimation(){
    //scaleFactor/5;
    //mid.x, mid.y*1.75
    PVector ps;
    for (int j = 0; j < 20; j++){
      ps = new PVector(random(mid.x-scaleFactor/3, mid.x+scaleFactor/3), random(mid.y*1.75-scaleFactor/6, mid.y*1.75+scaleFactor/6));
      for(int i = 0; i < particleDensity; i++){
        Particle p = new Particle("ball", new PVector(ps.x, ps.y), (playerSize/3)*2, random(360), 3, random(5, 100), collectColor);
        particles.add(p);
      }
    }
    //recordSound.play();
}
