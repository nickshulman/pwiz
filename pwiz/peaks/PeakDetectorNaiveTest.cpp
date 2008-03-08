//
// PeakDetectorNaiveTest.cpp
//
//
// Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2006 Louis Warschaw Prostate Cancer Center
//   Cedars Sinai Medical Center, Los Angeles, California  90048
//   Unauthorized use or reproduction prohibited
//


#include "PeakDetectorNaive.hpp"
#include "data/FrequencyData.hpp"
#include "data/PeakData.hpp"
#include "util/unit.hpp"
#include <iostream>
#include <iterator>


using namespace std;
using namespace pwiz::util;
using namespace pwiz::peaks;
using namespace pwiz::data;
using namespace pwiz::data::peakdata;


ostream* os_ = 0;


void testCreation()
{
    const double noiseFactor = 666;
    const unsigned int detectionRadius = 13;
    auto_ptr<PeakDetectorNaive> pd = PeakDetectorNaive::create(noiseFactor, detectionRadius);
    unit_assert(pd->noiseFactor() == noiseFactor);
    unit_assert(pd->detectionRadius() == detectionRadius);
}


FrequencyDatum data_[] = 
{
    FrequencyDatum(0, 0), 
    FrequencyDatum(1, 1), // peak radius 1
    FrequencyDatum(2, 0),
    FrequencyDatum(3, 1),
    FrequencyDatum(4, 2), // peak radius 2
    FrequencyDatum(5, 1),
    FrequencyDatum(6, 0),
    FrequencyDatum(7, 1),
    FrequencyDatum(8, 2),
    FrequencyDatum(9, 3), // peak radius 3
    FrequencyDatum(10, 2),
    FrequencyDatum(11, 1),
    FrequencyDatum(12, 0)
};


const unsigned int dataSize_ = sizeof(data_)/sizeof(FrequencyDatum);


void testFind()
{
    FrequencyData fd;
    copy(data_, data_+dataSize_, back_inserter(fd.data()));
    if (os_) copy(fd.data().begin(), fd.data().end(), ostream_iterator<FrequencyDatum>(*os_, "\n"));
    fd.analyze();

    PeakData pd;
    pd.scans.resize(3);

    const double noiseFactor = 1;

    auto_ptr<PeakDetectorNaive> pdn1 = PeakDetectorNaive::create(noiseFactor, 1);
    pdn1->findPeaks(fd, pd.scans[0]);
    unit_assert(pd.scans[0].peakFamilies.size() == 3);

    auto_ptr<PeakDetectorNaive> pdn2 = PeakDetectorNaive::create(noiseFactor, 2);
    pdn2->findPeaks(fd, pd.scans[1]);
    unit_assert(pd.scans[1].peakFamilies.size() == 2);

    auto_ptr<PeakDetectorNaive> pdn3 = PeakDetectorNaive::create(noiseFactor, 3);
    pdn3->findPeaks(fd, pd.scans[2]);
    unit_assert(pd.scans[2].peakFamilies.size() == 1);

    if (os_)
    {
        *os_ << "pd:\n" << pd << endl;

        for (unsigned int i=0; i<pd.scans.size(); i++)
        {
            *os_ << "scan " << i << ":\n"; 
            pd.scans[i].printSimple(*os_);    
        }
    }
}


void test()
{
    testCreation();
    testFind();
}


int main(int argc, char* argv[])
{
    try
    {
        if (argc>1 && !strcmp(argv[1],"-v")) os_ = &cout;
        if (os_) *os_ << "PeakDetectorNaiveTest\n";
        test();
        return 0;
    }
    catch (exception& e)
    {
        cerr << e.what() << endl;
        return 1;
    }
}

