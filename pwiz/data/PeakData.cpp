//
// PeakData.cpp
//
//
// Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2007 Spielberg Family Center for Applied Proteomics 
//   Cedars Sinai Medical Center, Los Angeles, California  90048
//   Unauthorized use or reproduction prohibited
//


#include "PeakData.hpp"
//#include "util_old/MinimXML.hpp"
#include <complex>


// note: boost/archive headers must precede boost/serialization headers
// (as of Boost v1.33.1)
#include "boost/archive/xml_oarchive.hpp"
#include "boost/archive/xml_iarchive.hpp"
#include "boost/serialization/vector.hpp"


namespace boost {
namespace serialization {


template <typename Archive>
void serialize(Archive& ar, pwiz::data::peakdata::Peak& peak, const unsigned int version)
{
    ar & make_nvp("mz", peak.mz);
    ar & make_nvp("frequency", peak.frequency);
    ar & make_nvp("intensity", peak.intensity);
    ar & make_nvp("phase", peak.phase);
    ar & make_nvp("decay", peak.decay);
    ar & make_nvp("error", peak.error);
    ar & make_nvp("area", peak.area);
}


template <typename Archive>
void serialize(Archive& ar, pwiz::data::peakdata::PeakFamily& peakFamily, const unsigned int version)
{
    ar & make_nvp("mzMonoisotopic", peakFamily.mzMonoisotopic);
    ar & make_nvp("charge", peakFamily.charge);
    ar & make_nvp("peaks", peakFamily.peaks);
}


template <typename Archive>
void serialize(Archive& ar, pwiz::data::CalibrationParameters& cp, const unsigned int version)
{
    ar & make_nvp("A", cp.A);
    ar & make_nvp("B", cp.B);
}


template <typename Archive>
void serialize(Archive& ar, pwiz::data::peakdata::Scan& scan, const unsigned int version)
{
    ar & make_nvp("scanNumber", scan.scanNumber);
    ar & make_nvp("retentionTime", scan.retentionTime);
    ar & make_nvp("observationDuration", scan.observationDuration);
    ar & make_nvp("calibrationParameters", scan.calibrationParameters);
    ar & make_nvp("peakFamilies", scan.peakFamilies);
}


template <typename Archive>
void serialize(Archive& ar, pwiz::data::peakdata::Software& software, const unsigned int version)
{
    ar & make_nvp("name", software.name);
    ar & make_nvp("version", software.version);
    ar & make_nvp("source", software.source);

    // don't know why archiving "map" chokes
    //ar & make_nvp("parameters", software.parameters);
}


template <typename Archive>
void serialize(Archive& ar, pwiz::data::peakdata::PeakData& pd, const unsigned int version)
{
    ar & make_nvp("sourceFilename", pd.sourceFilename);
    ar & make_nvp("software", pd.software);
    ar & make_nvp("scans", pd.scans);
}

} // namespace serialization
} // namespace boost


namespace pwiz {
namespace data {
namespace peakdata {


using namespace std;
//using namespace pwiz::util;


void PeakFamily::printSimple(std::ostream& os) const
{
    if (peaks.empty())
        os << 0 << " " << complex<double>(0.) << " " << 0 << endl;
    else
        os << peaks[0].frequency << " "
           << polar(peaks[0].intensity, peaks[0].phase) << " "
           << charge << endl; 
}


void Scan::printSimple(std::ostream& os) const
{
    for (vector<PeakFamily>::const_iterator it=peakFamilies.begin(); it!=peakFamilies.end(); ++it)
        it->printSimple(os);
}


namespace {

/*
void writeNameValuePair(MinimXML::Writer& writer, const string& name, const string& value)
{
    writer.setStyleFlags(MinimXML::ElementOnSingleLine);
    writer.startElement(name);
    writer.data(value.c_str());
    writer.endElement();
    writer.setStyleFlags(0);
}

void writeNameValuePair(MinimXML::Writer& writer, const string& name, double value)
{
    ostringstream oss;
    oss << value;
    writeNameValuePair(writer, name, oss.str());
}
*/
/*
void writeCalibrationParameters(MinimXML::Writer& writer, const CalibrationParameters& cp)
{
    writer.pushAttribute("A", cp.A); 
    writer.pushAttribute("B", cp.B); 
    writer.startAndEndElement("calibrationParameters");
}

void writePeak(MinimXML::Writer& writer, const Peak& peak)
{
    writer.pushAttribute("mz", peak.mz);
    writer.pushAttribute("intensity", peak.intensity);
    writer.pushAttribute("area", peak.area);
    writer.pushAttribute("error", peak.error);
    writer.pushAttribute("frequency", peak.frequency);
    writer.pushAttribute("phase", peak.phase);
    writer.pushAttribute("decay", peak.decay);
    writer.startAndEndElement("peak");
}

void writePeakFamily(MinimXML::Writer& writer, const PeakFamily& peakFamily)
{
    writer.pushAttribute("mzMonoisotopic", peakFamily.mzMonoisotopic);
    writer.pushAttribute("charge", (long)peakFamily.charge);
    writer.startElement("peakFamily");

    writer.pushAttribute("count", (long)peakFamily.peaks.size());
    writer.startElement("peaks");
    for (vector<Peak>::const_iterator it=peakFamily.peaks.begin(); it!=peakFamily.peaks.end(); ++it)
        writePeak(writer, *it);
    writer.endElement();

    writer.endElement();
}

void writeScan(MinimXML::Writer& writer, const Scan& scan)
{
    writer.pushAttribute("scanNumber", (long)scan.scanNumber);
    writer.pushAttribute("retentionTime", scan.retentionTime);
    writer.pushAttribute("observationDuration", scan.observationDuration);
    writer.startElement("scan");

    writeCalibrationParameters(writer, scan.calibrationParameters);

    writer.pushAttribute("count", (long)scan.peakFamilies.size());
    writer.startElement("peakFamilies");
    for (vector<PeakFamily>::const_iterator it=scan.peakFamilies.begin(); it!=scan.peakFamilies.end(); ++it)
        writePeakFamily(writer, *it);
    writer.endElement();

    writer.endElement();
}

void writeSoftware(MinimXML::Writer& writer, const Software& software)
{
    writer.pushAttribute("name", software.name);
    writer.pushAttribute("version", software.version);
    writer.pushAttribute("source", software.source);
    writer.setStyleFlags(MinimXML::AttributesOnMultipleLines);
    writer.startElement("software");
    writer.setStyleFlags(0);

    writer.pushAttribute("count", (long)software.parameters.size());
    writer.startElement("parameters");
    for (Software::Parameters::const_iterator it=software.parameters.begin();
         it!=software.parameters.end(); ++it)
    {
        writer.pushAttribute("name", it->first);
        writer.pushAttribute("value", it->second);
        writer.startAndEndElement("parameter");
    }
    writer.endElement();

    writer.endElement();
}
*/
} // namespace


void PeakData::writeXML(std::ostream& os) const
{
    os.precision(12);

    throw runtime_error("[PeakData::writeXML()] Needs to be reimplemented.");
/*
    auto_ptr<MinimXML::Writer> writer(MinimXML::Writer::create(os));
    writer->prolog();

    ostringstream versionString;
    versionString << PeakDataFormatVersion_Major << "." << PeakDataFormatVersion_Minor;

    writer->pushAttribute("version", versionString.str());
    writer->pushAttribute("sourceFilename", sourceFilename);
    writer->setStyleFlags(MinimXML::AttributesOnMultipleLines);
    writer->startElement("peakdata");
    writer->setStyleFlags(0);

    writeSoftware(*writer, software);

    // TODO: write generic writer for containers
    writer->pushAttribute("count", (long)scans.size());
    writer->startElement("scans");
    for (vector<Scan>::const_iterator it=scans.begin(); it!=scans.end(); ++it)
        writeScan(*writer, *it);
    writer->endElement();

    writer->endElement();
*/
}


using boost::serialization::make_nvp;
using boost::archive::xml_iarchive;
using boost::archive::xml_oarchive;


std::ostream& operator<<(std::ostream& os, const Peak& peak)
{
    os << "<"
       << peak.mz << ","
       << peak.intensity << ","
       << peak.area << ","
       << peak.error << ","
       << peak.frequency << ","
       << peak.phase << ","
       << peak.decay << ">";

    return os;
}


std::ostream& operator<<(std::ostream& os, const PeakFamily& peakFamily)
{
    os << "peakFamily ("
       << "mzMonoisotopic:" << peakFamily.mzMonoisotopic << " "
       << "charge:" << peakFamily.charge << " "
       << "peaks:" << peakFamily.peaks.size() << ")\n"; 

    copy(peakFamily.peaks.begin(), peakFamily.peaks.end(), ostream_iterator<Peak>(os, "\n")); 
    return os;
}


std::ostream& operator<<(std::ostream& os, const Scan& scan)
{
    os << "scan (#" << scan.scanNumber 
       << " rt:" << scan.retentionTime
       << " T:" << scan.observationDuration
       << " A:" << scan.calibrationParameters.A
       << " B:" << scan.calibrationParameters.B << ")\n";
    copy(scan.peakFamilies.begin(), scan.peakFamilies.end(), ostream_iterator<PeakFamily>(os, "")); 
    return os;
}


std::ostream& operator<<(std::ostream& os, const PeakData& pd)
{
    xml_oarchive oa(os);
    oa << make_nvp("peakdata", pd);
    return os;
}


std::istream& operator>>(std::istream& is, PeakData& pd)
{
    xml_iarchive ia(is);
    ia >> make_nvp("peakdata", pd);
    return is;
}


} // namespace peakdata 
} // namespace data 
} // namespace pwiz


