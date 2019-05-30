#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_AMG88xx.h>

Adafruit_AMG88xx gridEye;

#define micInputPin 3
// audio sampling
const int sampleWindow = 50; // Sample window width in mS (50 mS = 20Hz). 
unsigned int sample; // instantenous sample 
char audioHighLow;

// output averaging  
const int numReadings = 20; // length of averaging array. Increase for further "smoothing"
int readings[numReadings]; // the readings from the analog input
int readIndex = 0; // the index of the current reading
int total = 0; // the running total
double average = 0; // averaged output

const int LedPin = 13;
int ledState = 1;
bool dataTransferComplete = false;
bool dataRequested = false;

float pixels[AMG88xx_PIXEL_ARRAY_SIZE];

bool checkDataRequest();
void GetAvgSoundLevel() ;
void sendGridEyeData();
void sendAudioData();
void TransferSerialData();
void intToBytes(byte *highBytes, byte *lowBytes, float data[], int length);
void writeHighLowBytes(byte highBytes[], byte lowBytes[], int numReadings);
double RMS_Voltage();

void setup()
{
    bool status;
    ledState = 0;
    pinMode(LedPin, OUTPUT);
    digitalWrite(LedPin, ledState);

    Serial.begin(115200);

    // default settings
    status = gridEye.begin();
    if (!status) {
        Serial.println("Could not find a valid AMG88xx sensor, check wiring!");
        while (1);
    }
    Serial.println("-- Pixels Test --");
    Serial.println();

    delay(100); // let sensor boot up
}

void loop()  {
    GetAvgSoundLevel();
    dataRequested = checkDataRequest();
    if (dataRequested) {
        gridEye.readPixels(pixels);
        TransferSerialData();
    }
    delay(10);
}

void GetAvgSoundLevel() {

    total = total - readings[readIndex]; // subtract the last reading:
    
    readings[readIndex] = RMS_Voltage(); // get RMS voltage from RMS_Voltage()

    total = total + readings[readIndex]; // add the reading to the total:
    
    readIndex = readIndex + 1; // advance to the next position in the array:

    if (readIndex >= numReadings) {  // When at end of array, 
        readIndex = 0; // loop to 0
    }
    
    average = total / numReadings; // calculate the average
    
    if (average > 0){
        audioHighLow = 'H';
    } 
    else {
        audioHighLow = 'L';
    }

    delay(1); // delay in between reads for stability
}

double RMS_Voltage() {  
  unsigned long startMillis= millis();  // Start of sample window
  unsigned int signalMax = 0; 
  unsigned int signalMin = 1024; 
  unsigned int RMS; // RMS value  
  double voltsRMS; // value to be returned
   
   while (millis() - startMillis < sampleWindow)  { // collect data for 50 mS
  
      sample = analogRead(micInputPin);
      if (sample < 1024)  // keep values less than 1024
      {
         if (sample > signalMax)
         {
            signalMax = sample;  // save just the max levels
         }
         else if (sample < signalMin)
         {
            signalMin = sample;  // save just the min levels
         }
      }
   }
  
  RMS = (signalMax - signalMin); ///sqrt(2); // RMS = pk-pk/sqrt(2)
  return voltsRMS = (RMS * 5.0) / 1024;  // convert to volts
}

bool checkDataRequest() {
    char startMarker = '1'; //ascii symbol for 1
    char dataReceived;

    while (Serial.available() > 0) {
        dataReceived = Serial.read();
        if (dataReceived == startMarker) {
            ledState = !ledState;  //toggle LED so we can see data transfer
            digitalWrite(LedPin, ledState);
            dataRequested = true;
        }
        else {
            dataRequested = false;
        }
    }

    return dataRequested;
}

void TransferSerialData() {
    sendGridEyeData();
    sendAudioData();
    dataRequested = false;
    ledState = 0;  //toggle LED so we can see data transfer
    digitalWrite(LedPin, ledState);
}

void sendGridEyeData() {
    uint16_t integerPixelVal[AMG88xx_PIXEL_ARRAY_SIZE];
    byte highBytes[AMG88xx_PIXEL_ARRAY_SIZE];
    byte lowBytes[AMG88xx_PIXEL_ARRAY_SIZE];

    intToBytes(highBytes, lowBytes, pixels,64);
    writeHighLowBytes(highBytes, lowBytes, AMG88xx_PIXEL_ARRAY_SIZE);
}

void sendAudioData() {
    Serial.write(audioHighLow);
    Serial.write("\n");
}

void intToBytes(byte *highBytes, byte *lowBytes, float data[], int length){
        uint16_t integerVal[AMG88xx_PIXEL_ARRAY_SIZE];
        for (int i=0;i<length;i++){
            integerVal[i] = uint16_t(pixels[i] * 100);
            highBytes[i] = integerVal[i] / 256;
            lowBytes[i] = integerVal[i] % 256;
        }
}

void writeHighLowBytes(byte highBytes[], byte lowBytes[], int numReadings){
    Serial.write(highBytes,numReadings);
    Serial.write(lowBytes,numReadings);
}
