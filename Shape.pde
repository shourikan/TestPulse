void Triangle(PVector pos, float size, String orientation) {
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
