#define HW Serial1
#define MAX_COUNTER 100

#include <Wire.h>
#include <LSM303.h>

LSM303 compass;
LSM303::vector<int16_t> running_min = {32767, 32767, 32767}, running_max = {-32768, -32768, -32768};
LSM303::vector<int16_t> temp_min = {0, 0, 0}, temp_max = {0, 0, 0};
int counter = 0;

char report[80];
int vib = 15;
char coords[10];
boolean callibrated = false;
float current_direction = 0;
boolean done = false;

struct Config {
  int vibration_duration;
  float direction_delta;
  int vibration_intensity;
};

Config conf;

void setup() {
  conf.vibration_duration = 2;
  conf.direction_delta = 10;
  conf.vibration_intensity = 4;
  
  pinMode(vib, OUTPUT);
  Serial.begin(9600);
  //Bluetooth Gold Module baud rate
  HW.begin(115200);
  
  // Comapass
  Wire.begin();
  compass.init();
  compass.enableDefault();
}

boolean CalibratedMagnetometer()
{
  if (callibrated) return true;
  compass.read();
  
  running_min.x = min(running_min.x, compass.m.x);
  running_min.y = min(running_min.y, compass.m.y);
  running_min.z = min(running_min.z, compass.m.z);

  running_max.x = max(running_max.x, compass.m.x);
  running_max.y = max(running_max.y, compass.m.y);
  running_max.z = max(running_max.z, compass.m.z);
  
  snprintf(report, sizeof(report), "min: {%+6d, %+6d, %+6d}    max: {%+6d, %+6d, %+6d}",
    running_min.x, running_min.y, running_min.z,
    running_max.x, running_max.y, running_max.z);
  Serial.println(report);
  
  // Check if temp and running are equal. They need to be equal MAX_COUNTER times before we
  // accept the value as true
  if (temp_min.x == running_min.x && temp_min.y == running_min.y && temp_min.z == running_min.z
      && temp_max.x == running_max.x && temp_max.y == running_max.y && temp_max.z == running_max.z)
  {
    if (++counter == MAX_COUNTER)
    {
      compass.m_min = running_min;
      compass.m_max = running_max;
      callibrated = true;
      Serial.println("YEZZIR");
    }
  }
  else
  {
    temp_min.x = running_min.x;
    temp_min.y = running_min.y;
    temp_min.z = running_min.z;
    
    temp_max.x = running_max.x;
    temp_max.y = running_max.y;
    temp_max.z = running_max.z;
  }
  
  delay(100);
  return callibrated;
}

void check_direction_changed()
{
  float heading_direction = compass.heading();
  if (abs(current_direction - heading_direction) > conf.direction_delta)
  {
    current_direction = heading_direction;
    snprintf(coords, sizeof(coords), "[%3d#%3d]",444,555);
    Serial.print(coords);
    Serial.print("[1]");
    Serial.println("");
    Serial.println(HW.print(coords));
    Serial.println(HW.print("[1]"));
  }
}

void vibrate()
{
  analogWrite(vib, conf.vibration_intensity * 51);
  delay(conf.vibration_duration * 1000);
  analogWrite(vib, 0);
}

void test_vibrate_loop()
{
  //analogWrite(vib, 0);    // sets the LED off
  digitalWrite(vib, HIGH);   // sets the LED on
  delay(2000);                  // waits for a second
  //analogWrite(vib, 0);    // sets the LED off
  digitalWrite(vib, LOW);    // sets the LED off
  delay(2000);
}

void send_ack()
{
  HW.print("[ACK]");
  HW.flush();
}

// TODO
void update_config()
{
  char s[10];
  while(HW.available() > 0)
  {
    char c = HW.read();
  }
  
  send_ack();
}

void loop() {
  //test_vibrate_loop();
  Serial.println("hello");
  delay(5000);
  // Read from bluetooth
  if (HW.available() > 0) 
  {
    int cmd = HW.read();
    Serial.print("HW received: ");
    Serial.println(cmd, DEC);
        
    switch(cmd)
    {
      case 1:
        Serial.println("vibrate");
        vibrate();
        break;
      case 2:
        //update_config();
        break;
      default:
        break;
    }
  }
}

