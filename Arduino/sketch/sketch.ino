// I2Cdev and MPU6050 must be installed as libraries, or else the .cpp/.h files
// for both classes must be in the include path of your project
// https://github.com/jrowberg/i2cdevlib
#include "I2Cdev.h"

#include "MPU6050_6Axis_MotionApps20.h"
//#include "MPU6050.h" // not necessary if using MotionApps include file

// Arduino Wire library is required if I2Cdev I2CDEV_ARDUINO_WIRE implementation
// is used in I2Cdev.h
#if I2CDEV_IMPLEMENTATION == I2CDEV_ARDUINO_WIRE
    #include "Wire.h"
#endif

HardwareSerial Uart = HardwareSerial();
int vib = 15;

void setup() {
  pinMode(vib, OUTPUT);
  Serial.begin(9600);
  Uart.begin(115200);
}

void loop() {
  int incomingByte;
  // Read from serial i.e. HTerm
  if (Serial.available() > 0) {
    incomingByte = Serial.read();
    Serial.print("USB received: ");
    Serial.println(incomingByte, DEC);
    Uart.print("USB received:");
    Uart.println(incomingByte, DEC);
  }
  // Read from bluetooth
  if (Uart.available() > 0) {
    incomingByte = Uart.read();
    Serial.print("UART received: ");
    Serial.println(incomingByte, DEC);
    Uart.print("UART received:");
    Uart.println(incomingByte, DEC);
    analogWrite(vib, 255);
    digitalWrite(vib, HIGH);
    delay(3000);
    analogWrite(vib, 0);
    //digitalWrite(vib, LOW);
  }
  delay(50);
}

