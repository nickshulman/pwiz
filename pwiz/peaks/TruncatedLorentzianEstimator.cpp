//
// TruncatedLorentzianEstimator.cpp
//
//
// Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2006 Louis Warschaw Prostate Cancer Center
//   Cedars Sinai Medical Center, Los Angeles, California  90048
//   Unauthorized use or reproduction prohibited
//


#include "TruncatedLorentzianEstimator.hpp"
#include "ParameterEstimator.hpp"
#include "math/Parabola.hpp"
#include <fstream>


namespace pwiz {
namespace peaks {


using namespace std;
using namespace math;
using namespace data;


class TruncatedLorentzianEstimatorImpl : public TruncatedLorentzianEstimator
{
    public:
    TruncatedLorentzianEstimatorImpl(); 
    virtual TruncatedLorentzianParameters initialEstimate(const FrequencyData& fd) const;
    virtual TruncatedLorentzianParameters iteratedEstimate(const FrequencyData& fd,
                                                           const TruncatedLorentzianParameters& tlp,
                                                           int iterationCount) const;

    virtual double error(const FrequencyData& fd, const TruncatedLorentzianParameters& tlp) const;
    virtual double normalizedError(const FrequencyData& fd, const TruncatedLorentzianParameters& tlp) const;
    virtual double sumSquaresModel(const FrequencyData& fd, const TruncatedLorentzianParameters& tlp) const;
    virtual void log(ostream* os) {log_ = os;}
    virtual void outputDirectory(const string& name) {outputDirectory_ = name;}


    private:
    ostream* log_;
    string outputDirectory_;
};


auto_ptr<TruncatedLorentzianEstimator> TruncatedLorentzianEstimator::create()
{
    return auto_ptr<TruncatedLorentzianEstimator>(new TruncatedLorentzianEstimatorImpl); 
}


TruncatedLorentzianEstimatorImpl::TruncatedLorentzianEstimatorImpl()
:   log_(&cout)
{}


namespace {
complex<double> initialAlphaEstimate(const FrequencyData& fd, double T, double tau, double f0)
{
    TruncatedLorentzian L(T);

    ublas::vector<double> p(4);   
    p(TruncatedLorentzian::AlphaR) = 1;
    p(TruncatedLorentzian::AlphaI) = 0;
    p(TruncatedLorentzian::Tau) = tau;
    p(TruncatedLorentzian::F0) = f0;

    complex<double> dataDotModel = 0;
    complex<double> modelDotModel = 0;

    for (FrequencyData::const_iterator it=fd.data().begin(); it!=fd.data().end(); ++it)
    {
        complex<double> l = L(it->x, p);
        dataDotModel += it->y * conj(l);
        modelDotModel += l * conj(l);
    }

    return dataDotModel / modelDotModel;
}
} // namespace


TruncatedLorentzianParameters TruncatedLorentzianEstimatorImpl::initialEstimate(const FrequencyData& fd) const
{
    TruncatedLorentzianParameters tlp;

    tlp.tau = tlp.T = fd.observationDuration();
    if (tlp.T == 0)
        throw runtime_error("[TruncatedLorentzianEstimatorImpl::initialEstimate()] T==0 in frequency data.");

    if (fd.data().size() < 3)
        throw runtime_error("[TruncatedLorentzianEstimatorImpl::initialEstimate()] Not enough data.");

    // find max, not including end points 
    FrequencyData::const_iterator max = fd.data().begin()+1;
    double maxAmplitude = abs(max->y); 
    for (FrequencyData::const_iterator it=fd.data().begin()+1; it!=fd.data().end()-1; ++it)
    {
        if (abs(it->y) > maxAmplitude)
        {
            max = it;
            maxAmplitude = abs(it->y);
        } 
    }

    // fit parabola to the 3 points surrounding max
    vector< pair<double,double> > samples;
    for (FrequencyData::const_iterator it=max-1; it!=max+2; ++it)
        samples.push_back(make_pair(it->x, 1/norm(it->y)));

    Parabola parabola(samples);
    tlp.f0 = parabola.center();

    tlp.alpha = initialAlphaEstimate(fd, tlp.T, tlp.tau, tlp.f0);

    return tlp;
}


TruncatedLorentzianParameters TruncatedLorentzianEstimatorImpl::iteratedEstimate(const FrequencyData& fd_in,
                                                                                 const TruncatedLorentzianParameters& tlp_in,
                                                                                 int iterationCount) const
{
    FrequencyData fd(fd_in, fd_in.data().begin(), fd_in.data().end());
    fd.normalize();

    TruncatedLorentzianParameters tlp = tlp_in;
    TruncatedLorentzian L(tlp.T);

    auto_ptr<ParameterEstimator> pe =
        ParameterEstimator::create(L, fd.data(), tlp.parameters(fd.shift(), fd.scale()));

    if (log_) *log_ << tlp << "\ninitial error: " << pe->error() << endl << endl;

    for (int i=1; i<=iterationCount; i++)
    {
        if (log_) *log_ << "Iteration " << i << endl;
        
        // iterate and get the new parameters 
        double errorChange = pe->iterate(log_);
        tlp.parameters(pe->estimate(), -fd.shift(), 1./fd.scale());

        if (log_)
        {
            *log_ << "parameters: " << tlp << endl;
            *log_ << "error: " << pe->error() << endl;
        }

        if (outputDirectory_ != "")
        {
            // write intermediate tlp file to outputDirectory
            ostringstream filename;
            filename << outputDirectory_ << "/" << i << ".tlp";
            *log_ << "Writing " << filename.str() << endl;
            tlp.write(filename.str());
        }

        if (errorChange == 0)
        {
            if (log_) *log_ << "No error change.\n\n";
            break;
        }

        if (log_) *log_ << endl;
    }

    return tlp;
}


double TruncatedLorentzianEstimatorImpl::error(const FrequencyData& fd, 
                                               const TruncatedLorentzianParameters& tlp) const
{
    double result = 0;

    TruncatedLorentzian L(tlp.T);
    ublas::vector<double> p = tlp.parameters();

    for (FrequencyData::const_iterator it=fd.data().begin(); it!=fd.data().end(); ++it)
    {
        double term = norm(it->y - L(it->x, p)); 
        //cout << it->x << " " << it->y << " " << L(it->x, p) << " " << term <<  endl;
        result += term; 
    }
    
    return result;
}


double TruncatedLorentzianEstimatorImpl::normalizedError(const FrequencyData& fd_in, 
                                                         const TruncatedLorentzianParameters& tlp_in) const
{
    FrequencyData fd(fd_in, fd_in.data().begin(), fd_in.data().end());
    fd.normalize();

    TruncatedLorentzianParameters tlp(tlp_in);
    tlp.parameters(tlp_in.parameters(fd.shift(), fd.scale()));

    return error(fd, tlp);
}


double TruncatedLorentzianEstimatorImpl::sumSquaresModel(const FrequencyData& fd, 
                                                         const TruncatedLorentzianParameters& tlp) const
{
    double result = 0;

    TruncatedLorentzian L(tlp.T);
    ublas::vector<double> p = tlp.parameters();

    for (FrequencyData::const_iterator it=fd.data().begin(); it!=fd.data().end(); ++it)
    {
        double term = norm(L(it->x, p)); 
        result += term; 
    }
    
    return result;
}


} // namespace peaks
} // namespace pwiz

