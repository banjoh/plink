HardwareSerial Uart = HardwareSerial();
int vib = 15;

void setup() {
    pinMode(vib, OUTPUT);
    Serial.begin(9600);
    Uart.begin(115200);
}

void loop() {
    int incomingByte;
    if (Serial.available() > 0) {
        incomingByte = Serial.read();
        Serial.print("USB received: ");
        Serial.println(incomingByte, DEC);
        Uart.print("USB received:");
        Uart.println(incomingByte, DEC);
    }
    if (Uart.available() > 0) {
        incomingByte = Uart.read();
        Serial.print("UART received: ");
        Serial.println(incomingByte, DEC);
        Uart.print("UART received:");
        Uart.println(incomingByte, DEC);
        analogWrite(vib, 255);
        //digitalWrite(vib, HIGH);
        delay(3000);
        analogWrite(vib, 0);
        //digitalWrite(vib, LOW);
    }
    delay(100);
}

