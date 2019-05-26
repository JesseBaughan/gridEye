/****************************************
Sound level measuring
Martin Cirson EGB340 Group 30
****************************************/
 
// audio sampling
const int sampleWindow = 50; // Sample window width in mS (50 mS = 20Hz). 
unsigned int sample; // instantenous sample 


// output averaging  
  const int numReadings = 20; // length of averaging array. Increase for further "smoothing"
  int readings[numReadings]; // the readings from the analog input
  int readIndex = 0; // the index of the current reading
  int total = 0; // the running total
  double average = 0; // averaged output

 
void setup() {
  Serial.begin(115200);
  
  for (int thisReading = 0; thisReading < numReadings; thisReading++) { 
    readings[thisReading] = 0; // initalise array to 0
  } 
}


double RMS_Voltage() {
  
  unsigned long startMillis= millis();  // Start of sample window
  unsigned int signalMax = 0; 
  unsigned int signalMin = 1024; 
  unsigned int RMS; // RMS value  
  double voltsRMS; // value to be returned
  int inputPin = 3;
   
   while (millis() - startMillis < sampleWindow)  { // collect data for 50 mS
  
      sample = analogRead(inputPin);
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


void smooth() { 

  total = total - readings[readIndex]; // subtract the last reading:
  
  readings[readIndex] = RMS_Voltage(); // get RMS voltage from RMS_Voltage()

  total = total + readings[readIndex]; // add the reading to the total:
  
  readIndex = readIndex + 1; // advance to the next position in the array:

  if (readIndex >= numReadings) {  // When at end of array, 
    readIndex = 0; // loop to 0
  }
  
  average = total / numReadings; // calculate the average

  Serial.println(average); 
  
  delay(1); // delay in between reads for stability

}

void loop() { 
  smooth();
}
