# The WRONJ problem

When computing a [perfectly parallel](https://en.wikipedia.org/wiki/Embarrassingly_parallel) workload, the **WRONJ (Workers Rest On Next Job)** problem is a performance problem that can occur when the [job scheduler](https://en.wikipedia.org/wiki/Job_scheduler) that assigns the workload jobs to idle workers runs in a single thread of execution. 

This problem usually goes unnoticed, but may be a big issue in the context of [grid computing](https://en.wikipedia.org/wiki/Grid_computing) when a grid has a large number of workers and the grid is full (i.e. the number of jobs to be done is much greater than the number of grid workers).

In these cases, we may want to reduce the workload total time and improve the grid performance by increasing the number of workers, but that's when the problem can arise: if the number of workers reaches a certain limit (***&#x2243; JT / AT***, where ***JT*** is the  average time for workload jobs and ***AT*** is the average time it takes for the scheduler to assign a new job from the job queue to an idle worker), the grid just doesn't scale: the total time will be the same from that limit on. 

 # Contents
- [WRONJ Conditions](#wronj-conditions)
- [Workload definitions](#workload-definitions)
- [Ideal grid](#ideal-grid)
- [WRONJ Worker Time](#wronj-worker-time)
    - [Constant times](#constant-times)
    - [Variable times](#variable-times)
- [More](#more)


## WRONJ Conditions

 1. We have a grid with a fixed number ***W*** of single-threaded worker processes (workers), all with the same computing power: they take the same time to compute the same job.
 1. The workload submitted to the grid is perfectly parallel.
 1. The grid uses a queue to manage all the pending jobs to compute. This is the ***JQ*** (Job Queue).
 1. The grid uses another queue to manage the idle workers, the ***FWQ*** (Free Workers Queue). When a worker has finished its job, the grid add an identifier of the worker to the ***FWQ***.
 1. The ***JS*** (Job Scheduler) of the grid is a single-threaded process, that chooses the next job to execute from the ***JQ*** and assigns it to the first idle worker in the ***FWQ***. The ***at*** (assignment time) is the time it takes for the ***JS***  to complete this task for a job, and the ***AT*** (Assignment Time) is the average of all ***at*** in the workload.
 
 The ***at*** is made up of the scheduling algorithm time plus the time it takes to send the job data to the worker, probably over a network. A realistic set of values to these parameter would be ***AT***=1 millisecond for and ***w***=1000, but for the sake of clarity in most of the examples in this document we'll use a grid with only 10 workers and ***AT***=1 second, with a [FCFS](https://en.wikipedia.org/wiki/Scheduling_(computing)#First_come,_first_served) scheduling algorithm, like this one: 

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/beforeAssignment.png)
 
In the samples, we'll use a different icon for each job, and a different color for each worker (ranging from red to blue). In the image above, we can see seven active workers computing a job, and three idle workers waiting for a job. The ***JS*** will now get the next job in the ***JS*** and assign it to the first worker in the ***FWQ*** (the orange one), making it an active worker:

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/afterAssignment.png)

The WRONJ problem occurs when there are active workers finishing its job at the time the ***JS*** is assigning a new job, so the ***FWQ*** will never be empty and the grid won't use 100% of its capacity, like this:

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/finishJob.png)


## Workload Definitions
 
 1. ***J*** : number of jobs in the workload.
 1. ***jt*** (job time): computing time of a job in a worker.
 2. ***wt*** (worker time): time it takes a worker to process a job. It's made up of the ***jt*** plus the time that the worker has been waiting in the ***FWQ*** until the ***JS*** assigned the job, so its minimum value is ***jt*** + ***at***.
 1. ***JT*** (Job Time): average of all ***jt*** in the workload, 
 1. ***WT*** (Worker Time): average of all ***wt*** in the workload, its minimum value is ***JT*** + ***AT***.
 1. ***TT*** (Total Time): is the time it takes the grid to process all the jobs in the workload. When ***J*** >> ***W***, this time will be ***&#x2243; J*** * ***WT / W***.

## Ideal grid

An ideal grid is one where ***at*** is 0, so ***WT = JT*** and the total time of a workload with ***J*** >> ***W*** will be ***&#x2243; J*** * ***JT / W***.

## WRONJ Worker Time 

In a grid with a fixed fixed number ***W*** of workers, the WRONJ problem occurs if the workload ***JT*** falls bellow the limit set by ***AT*** * ***(W -1)*** :

- If ***JT*** >= ***AT*** * ***(W -1)*** , then ***WT*** has its optimum value of:
    - ***WT = JT + AT***.
- If ***JT*** < ***AT*** * ***(W -1)*** , then ***WT > JT + AT***. In fact, no matter how low ***JT*** may go, ***WT*** remains fixed at this value: 
    - ***WT = W * AT***.




### Constant Times 

 - Simulation :

![alt text](https://raw.githubusercontent.com/jormigla/images_test/master/simul10constant.gif)

 - Time Diagram fot JT < Limit :

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/dt_lt_limit.png) 

 - Time Diagram fot JT >= Limit :

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/dt_gt_limit.png)


### Variable Times 

 - Simulation :

![alt text](https://raw.githubusercontent.com/jormigla/images_test/master/simul10var.gif)


<div id="more"></div>

## More

