class Obstacle{    
	
	PVector pos;
	float speedMult = 1;
	color c;
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
	
	void Move(PVector posIn) {
		pos.add(new PVector(posIn.x * speedMult, posIn.y * speedMult));
	}
	
	void Draw() {
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
	
	void ResetPos() {
		PVector rand = new PVector(random(- width, width * 2), random(- height, 0));
		pos = rand;
	}
	
} 