# The WRONJ problem

The **WRONJ (Workers Rest On Next Job)** problem occurs when a [job scheduler](https://en.wikipedia.org/wiki/Job_scheduler) has a single process for assigning new jobs to idle workers. This problem usually goes unnoticed, but may be a big issue in the context of [Grid computing](https://en.wikipedia.org/wiki/Grid_computing) when the grid is full (i.e. the number of jobs to be done is much greater than the number of grid workers).

In these cases, we may want to improve the performance of the grid by increasing the number of workers, but that's when the problem may arise: if the number of workers reaches a certain limit (***&#x2243; jt / at***, where ***jt*** is the  average time of the jobs and ***at*** is the average time that takes to the scheduler assigning a new job from the job queue to an idle worker ), the grid just don't scale: the performance is the same from that limit on. 

 # Contents
1. [Conditions](#conditions)
2. [Constant times](#contant-times)
2. [Variable times](#variable-times)
4. [More](#more)

<div id="conditions"></div>

## Conditions


 
<div id="contant-times"></div>

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

