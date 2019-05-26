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

// double RMS_Voltage() { 
//   unsigned long startMillis= millis();  // Start of sample window
//   unsigned int signalMax = 0; 
//   unsigned int signalMin = 1024; 
//   unsigned int RMS; // RMS value  
//   double voltsRMS; // value to be returned
//   int inputPin = 3;
   
//    while (millis() - startMillis < sampleWindow)  { // collect data for 50 mS
//       sample = analogRead(inputPin);
//       if (sample < 1024)  // keep values less than 1024
//       {
//          if (sample > signalMax)
//          {
//             signalMax = sample;  // save just the max levels
//          }
//          else if (sample < signalMin)
//          {
//             signalMin = sample;  // save just the min levels
//          }
//       }
//    } 
//   RMS = (signalMax - signalMin); ///sqrt(2); // RMS = pk-pk/sqrt(2)
//   return voltsRMS = (RMS * 5.0) / 1024;  // convert to volts  
// }

// void smooth() { 
//   total = total - readings[readIndex]; // subtract the last reading:
//   readings[readIndex] = RMS_Voltage(); // get RMS voltage from RMS_Voltage()
//   total = total + readings[readIndex]; // add the reading to the total:

//   readIndex = readIndex + 1; // advance to the next position in the array:

//   if (readIndex >= numReadings) {  // When at end of array, 
//     readIndex = 0; // loop to 0
//   }
  
//   average = total / numReadings; // calculate the average

//   Serial.println(average); 
//   delay(1); // delay in between reads for stability
// }

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


