# The WRONJ problem

The **WRONJ (Workers Rest On Next Job)** problem occurs when a [job scheduler](https://en.wikipedia.org/wiki/Job_scheduler) has a single process for assigning new jobs to idle workers. This problem usually goes unnoticed, but may be a big issue in the context of [Grid computing](https://en.wikipedia.org/wiki/Grid_computing) when the grid is full (i.e. the number of jobs to be done is much greater than the number of grid workers).

In these cases, we may want to improve the performance of the grid by increasing the number of workers, but that's when the problem may arise: if the number of workers reaches a certain limit (***&#x2243; jt / at***, where ***jt*** is the  average time of the jobs and ***at*** is the average time that takes to the scheduler assigning a new job from the job queue to an idle worker ), the grid just don't scale: the performance is the same from that limit on. 

 # Contents
1. [WRONJ Conditions](#conditions)
2. [Constant times](#constant-times)
2. [Variable times](#variable-times)
4. [More](#more)

<div id="conditions"></div>

## WRONJ Conditions

 1. We have a grid with a fixed number ***w*** of single-threaded worker processes (workers), all with the same computing power: they take the same time to compute the same job.
 2. The grid uses a queue to manage all the pending jobs to compute. This is the ***JQ*** (Job Queue).
 3. The grid uses another queue to manage the idle workers, the ***FWQ*** (Free Workers Queue). When a worker has finished its job, the grid add an identifier of the worker to the ***FWQ***.
 4. The ***JS*** (Job Scheduler) of the grid is a single-threaded process, that chooses the next job to execute from the ***JQ*** and assigns it to the first idle worker in the ***FWQ***. The ***AT*** (Assignment Time) is the average time it takes for the ***JS***  to complete this task.
 
 The ***AT*** is made up of the scheduling algorithm time plus the time it takes to send the task to the worker, probably over a network. A realistic set of values to these parameter would be ***AT***=1 millisecond for and ***w***=1000, but for the sake of clarity we'll use in this document a grid with just 10 workers and ***AT***=1 second. 

 
 
<div id="constant-times"></div>

## Constant Times 

 - Simulation :

![alt text](https://raw.githubusercontent.com/jormigla/images_test/master/simul10constant.gif)

 - Time Diagram fot JT < Limit :

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/dt_lt_limit.png) 

 - Time Diagram fot JT >= Limit :

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/dt_gt_limit.png)


<div id="variable-times"></div>

## Variable Times 

 - Simulation :

![alt text](https://raw.githubusercontent.com/jormigla/images_test/master/simul10var.gif)


<div id="more"></div>

## More

