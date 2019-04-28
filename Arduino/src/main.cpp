#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_AMG88xx.h>

Adafruit_AMG88xx amg;

const int LedPin = 13;  
int ledState = 1;  
boolean dataTransferComplete = false;

float pixels[AMG88xx_PIXEL_ARRAY_SIZE];

void sendSerialData();
void sendHighLowBytes(); 

void setup()  
{  
    bool status; 
    ledState = 1; 
    pinMode(LedPin, OUTPUT); 
    digitalWrite(LedPin, ledState); 

    Serial.begin(115200); 
    
    // default settings
    status = amg.begin();
    if (!status) {
        Serial.println("Could not find a valid AMG88xx sensor, check wiring!");
        while (1);
    } 
    Serial.println("-- Pixels Test --");
    Serial.println(); 

    delay(100); // let sensor boot up  
}  
  
void loop()  {   
    amg.readPixels(pixels);
    sendSerialData();  

    //delay a second
    delay(2000);     
}

void sendSerialData() {
    char startMarker = '1'; //ascii symbol for 1 
    char dataReceived;
     
    while (Serial.available() > 0 && dataTransferComplete == false) { 
        dataReceived = Serial.read();   
        if (dataReceived == startMarker) {
            sendHighLowBytes();
            dataTransferComplete == true;    
        }
        ledState = !ledState;  
        digitalWrite(LedPin, ledState);
    }
}

void sendHighLowBytes() {
    byte highBytes[64], lowBytes[64];
    uint16_t integerPixelVal;

    for(int i=0; i < AMG88xx_PIXEL_ARRAY_SIZE; i++){
        //convert from float to int, will need to convert back once host receives
        integerPixelVal = uint16_t(pixels[i] * 100);
        highBytes[i] = integerPixelVal / 256;
        lowBytes[i] = integerPixelVal % 256;
    }  
    Serial.write(highBytes,64);
    Serial.write(lowBytes,64);
    //Serial.flush();
}


