void DBInit(){
  
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

boolean CreateUser(String name){
  return db.execute("INSERT into data (`name`,`score`) VALUES ('"+name+"', 0);");
}

String[] GetScoreboard(){
  
  ArrayList<String> results = new ArrayList<String>();
  int count = 3;
  
  db.query("SELECT score FROM data ORDER BY score DESC;");
  
  while (db.next () && count-- > 0){
    results.add(db.getString("score"));
  }
  
  return results.toArray(new String[0]);
}

int GetBest(){
  db.query("SELECT score FROM data WHERE name='record';");
  
  if (db.next ())
    return db.getInt("score");
    
  return 0;
}

void SetBest(int best){
  db.execute("UPDATE data SET score="+best+" WHERE name='record';");
}
