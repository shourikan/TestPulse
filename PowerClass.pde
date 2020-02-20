class Power extends Obstacle{
  
  String type;
  
  Power (){
    super();
  }

  void DrawP (){
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
