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
	
	void DrawC() {
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
