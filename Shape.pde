void Triangle(PVector pos, float size, String orientation){
  switch(orientation){
    case "lefttop":{
      triangle(pos.x-size, pos.y-size, pos.x+size+size/3, pos.y+size-size/3, pos.x+size-size/3, pos.y+size+size/3);
    }
    case "leftbot":{
      triangle(x1, y1, x2, y2, x3, y3);
    }
    case "left":{
      triangle(x1, y1, x2, y2, x3, y3);
    }
    case "righttop":{
      triangle(pos.x+size, pos.y-size, pos.x-size-size/3, pos.y+size-size/3, pos.x-size+size/3, pos.y+size+size/3);
    }
    case "rightbot":{
      triangle(x1, y1, x2, y2, x3, y3);
    }
    case "right":{
      triangle(x1, y1, x2, y2, x3, y3);
    }
    case "top":{
      triangle(pos.x, pos.y-size, pos.x+size/3, pos.y+size-size/3, pos.x+size-size/3, pos.y+size+size/3);
    }
    case "bot":{
      triangle(x1, y1, x2, y2, x3, y3);
    }
  }
}