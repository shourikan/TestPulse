void GameOver() {
	//bgSound.stop();
	//overSound.play();
	best = GetBest();
	particles = new ArrayList<Particle>();
	if (score > best) {
		SetBest(int(score));
		RecordAnimation();
		best = GetBest();
	}
	
	game = false;
	endMenu = true;
}

void Start() {
	if (paired && !ready) {
		sendPlay();
		ready = true;
	}
	
	Reset();
	
	if ((paired && triggerPlay) || !paired) {
		ready = false;
		triggerPlay = false;
		startMenu = false;
		endMenu = false;
		settingsMenu = false;
		game = true;
	}
}

void Reset() {
	
	obstacles = new ArrayList<Obstacle>();
	collects = new ArrayList<Collect>();
	powers = new ArrayList<Power>();
	particles = new ArrayList<Particle>();
	b = false;
	winner = true;
	
	//Position
	realPos = new PVector(width / 2, height / 2);
	multiplayerPos = new PVector(width / 2, height / 2);
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