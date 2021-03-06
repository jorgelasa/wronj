// WRONJCpp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//
#include <iostream>
#include <set>
#include <cmath>
#include <vector>
#include <numeric>
#include <memory>
#include <random>

using namespace std;

int main()
{
    int workers = 256;
    int jobsNumber = 256000;
    double assignmentTimeMean = 0.00066;
    double jobTimeMean = 0.017;
    double jobTimeVolatility = 0;
    double assignmentTimeVolatility = 0;
    jobTimeVolatility = 0.05;
    //assignmentTimeVolatility = 0.2;
    std::unique_ptr<std::lognormal_distribution<>> atDist, jtDist;
    std::mt19937 gen;
    if (assignmentTimeVolatility > 0) {
        // According to https://en.wikipedia.org/wiki/Log-normal_distribution, we can calculate
        // mu and sigma from actual mean and volatility this way
        double s = std::sqrt(std::log(1 + std::pow(assignmentTimeVolatility / assignmentTimeMean, 2)));
        double m = std::log(assignmentTimeMean) - std::pow(s, 2)/2;
        atDist.reset(new std::lognormal_distribution<>(m, s));
    }
    if (jobTimeVolatility > 0) {
        double s = std::sqrt(std::log(1 + std::pow(jobTimeVolatility / jobTimeMean, 2)));
        double m = std::log(jobTimeMean) - std::pow(s, 2)/2;
        jtDist.reset(new std::lognormal_distribution<>(m, s));
    }
    //Ending times of jobs in workers    
    multiset<double> workersTime;
    double theoreticTime = jobsNumber * jobTimeMean / workers;
    double modelTaskDelay = assignmentTimeMean * (workers - 1) >  jobTimeMean ?
        assignmentTimeMean * workers -  jobTimeMean : assignmentTimeMean;
    double modelTime = assignmentTimeMean * (workers - 1) >  jobTimeMean ?
        assignmentTimeMean * workers  : jobTimeMean + assignmentTimeMean ;
    double time = 0, workersTimeMean = 0, jobsTimeMean = 0;
    for (long j = 1; j <= jobsNumber; j++) {
        double lastWorkerEndTime = time;
        if (workersTime.size() == workers) {
            auto firstWorker = workersTime.begin();
            lastWorkerEndTime = *firstWorker;
            if (lastWorkerEndTime > time)
                time = lastWorkerEndTime;
            workersTime.erase(firstWorker);
        }
        time += (atDist ? (*atDist)(gen) : assignmentTimeMean) ;
        double workerEndTime = (jtDist ? (*jtDist)(gen) : jobTimeMean);
        jobsTimeMean = ((j - 1) * jobsTimeMean + workerEndTime) / j;
        workerEndTime += time;
        workersTimeMean = ((j - 1) * workersTimeMean + (workerEndTime - lastWorkerEndTime)) / j;
        workersTime.insert(workerEndTime);
    }
    time = *(--workersTime.end());
    double realTaskDelay =  (time - theoreticTime) * workers / jobsNumber;
    double adjustedModelTime = assignmentTimeMean * (workers - 1) >  jobsTimeMean ?
        assignmentTimeMean * workers  : jobsTimeMean + assignmentTimeMean ;
    cout << fixed <<
        "\nTheoretic time: " << theoreticTime <<
        "\nReal time: " << time <<
        "\nWorkers: " << workers <<
        "\nNumber of jobs: " << jobsNumber <<
        "\nAssignment time volatility: " << assignmentTimeVolatility <<
        "\nAssignment time mean: " << assignmentTimeMean <<
        "\nat*workers (ms): " << assignmentTimeMean * workers <<
        "\nat*(workers -1) (ms): " << assignmentTimeMean * (workers - 1) <<
        "\nReal task delay (ms): " << realTaskDelay <<
        "\nModel task delay (ms): " << modelTaskDelay <<
        "\nJob time volatility: " << jobTimeVolatility <<
        "\nJob time mean (seconds) : " << jobTimeMean <<
        "\nJobs time mean (seconds) : " << jobsTimeMean <<
        "\nModel time (seconds) : " << modelTime <<
        "\nAdjusted Model time (seconds): " << adjustedModelTime <<
        "\nWorkers time mean (seconds) : " << workersTimeMean <<
        endl;
}

