# The WRONJ problem

When computing a [perfectly parallel](https://en.wikipedia.org/wiki/Embarrassingly_parallel) workload, the **WRONJ (Workers Rest On Next Job)** problem is a performance slowdown that can occur when the [job scheduler](https://en.wikipedia.org/wiki/Job_scheduler) that assigns the workload jobs to idle workers runs in a single thread of execution. 

This is a generic scheduling problem and usually goes unnoticed, but may be a big issue in the context of high throuhput [grid computing](https://en.wikipedia.org/wiki/Grid_computing) when a grid has a large number of workers and the grid is full (i.e. the number of jobs to be done is much greater than the number of grid workers).

In these cases, we may want to reduce the workload total time and improve the grid performance by increasing the number of workers, but that's when the problem can arise: if the number of workers reaches a certain limit (***&#x2243; JT / AT***, where ***JT*** is the  average time for workload jobs and ***AT*** is the average time it takes for the scheduler to assign a new job from the job queue to an idle worker), the grid just doesn't scale: the total time will be the same from that limit on. 

It's basically a [parallel slowdown](https://en.wikipedia.org/wiki/Parallel_slowdown) affecting a kind of workloads where these slowdowns are not supposed to happen on an ideal grid, but can actually appears when we hit some thresold values on a wrongly implemented grid.

 # Contents
- [WRONJ Conditions](#wronj-conditions)
- [Workload definitions](#workload-definitions)
- [Ideal grid vs WRONJ grid](#ideal-grid-vs-wronj-grid)
- [WRONJ Worker Time](#wronj-worker-time)
    - [WWT proof](#wwt-proof)
    - [WWT graphical proof with constant times](#wwt-graphical-proof-with-constant-times)
- [Possible solutions to the WRONJ problem](#possible-solutions-to-the-wronj-problem)
- [The WRONJ app](#the-wronj-app)

# WRONJ Conditions

 1. We have a grid with a number ***W*** of single-threaded worker processes (workers), all with the same computing power: they take the same time to compute the same job.
 1. The workload submitted to the grid is perfectly parallel.
 1. The grid uses a queue to manage all the pending jobs to compute. This is the ***JQ*** (Job Queue).
 1. The grid uses another queue to manage the idle workers, the ***FWQ*** (Free Workers Queue). When a worker has finished its job, the grid add a reference of that worker to the end of the ***FWQ***.
 1. The ***JS*** (Job Scheduler) of the grid is a single-threaded process, that chooses the next job to execute from the ***JQ*** and assigns it to the first idle worker in the ***FWQ***. The ***at*** (assignment time) is the time it takes for the ***JS***  to complete this task for a job, and the ***AT*** (Assignment Time) is the average of all ***at*** in the workload.
 
 The ***at*** is made up of the scheduling algorithm time plus the time it takes to send the job data to the worker, probably over a network. A realistic set of values to these parameter would be ***AT***=1 millisecond and ***W***=1000, but for the sake of clarity in most of the examples in this document we'll use a grid with only 10 workers and ***AT*** >= 100 milliseconds, with a [FCFS](https://en.wikipedia.org/wiki/Scheduling_(computing)#First_come,_first_served) scheduling algorithm, like this one: 

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/beforeAssignment.png)
 
In the samples, we'll use a different icon for each job, and a different color for each worker (ranging from red to blue). In the image above, we can see seven active workers computing different jobs, and three idle workers waiting for a new job. The ***JS*** will now get the next job in the ***JS*** and assign it to the first worker in the ***FWQ*** (the orange one), making it an active worker:

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/afterAssignment.png)

The **WRONJ** problem occurs when there are active workers finishing its job at the time the ***JS*** is assigning a new job, so the ***FWQ*** will never be empty and the grid won't use 100% of its capacity, like this:

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/finishJob.png)


# Workload Definitions
 
In some of the following concepts, we will use the same name to define individual and average values (using capital letters in the latter case):

 1. ***J*** : number of jobs in the workload.
 1. ***fw*** (free workers): number of workers in the ***FWQ***.
 1. ***FW*** (Free Workers): average of all values of ***fw*** in the workload.
 1. ***aw*** (active workers): number of active workers: ***aw = W - fw***.
 1. ***AW*** (Active Workers): average of all values of ***aw*** in the workload: ***AW = W - FW***.
 1. ***jt*** (job time): computing time of a job in a worker.
 1. ***JT*** (Job Time): average of all ***jt*** in the workload. 
 1. ***fwqt*** (free worker queue time): time that the worker has been waiting in the ***FWQ*** until the ***JS*** assigned it a job. Its minimum value is ***at***.
 1. ***FWQT*** (Free Worker Queue  Time): average of all ***fwqt*** in the workload. Its minimum value is ***AT***.
 1. ***wt*** (worker time): time it takes a worker to process a job. It's ***wt = jt + fwqt***, so its minimum value is ***jt*** + ***at***.
 1. ***WT*** (Worker Time): average of all ***wt*** in the workload. ***WT = JT + FWQT*** and its minimum value is ***JT*** + ***AT***.
 1. We'll say that ***the grid is full*** when ***J*** >> ***W***.
 1. ***TT*** (Total Time): is the time it takes the grid to process all the jobs in the workload. When the grid is full, this time will be ***&#x2243; J*** * ***WT / W***.
 1. ***WWT*** (WRONJ Worker Time): Worker Time in a **WRONJ** grid that ids full.


# Ideal grid vs WRONJ grid

An ideal grid is one where ***at = 0***: all workers are always active when the grid is full. In such a grid, the equality ***WT = JT*** holds, and the total time of a workload will be ***&#x2243; J*** * ***JT / W***.

A **WRONJ** grid is one that meets the [conditions](#wronj-conditions) above, with ***at > 0***. The ***WT*** in these grids has an odd behavior, as we are about to see.

# WRONJ Worker Time

The **WRONJ** problem is due to this dual value of ***WT*** in a **WRONJ** grid when it's full:

1. If ***JT >= (W -1) * AT*** , then ***WT*** is approximately equal to its minimum value:
    - ***WT &#x2243; JT + AT***.
2. If ***JT < (W -1) * AT*** , then ***WT*** has this value (greater than its minimum value): 
    - ***WT &#x2243; W * AT***.

When the grid is full the total time of a workoad is ***TT&#x2243; J * WT / W***, so the above expressions imply that:

1. If ***JT >= (W -1) * AT*** , then:
    - ***TT &#x2243; J * (JT + AT) / W***
2. If ***JT < (W -1) * AT*** , then: 
    - ***TT &#x2243; J * AT*** 

In a grid with a fixed number ***W*** of workers, the **WRONJ** problem occurs when the workload ***JT*** falls bellow this limit ***(W -1) * AT***. In fact, no matter how low ***JT*** may go, ***WT*** remains fixed at the value ***W * AT*** and ***TT*** remains fixed at ***J * AT***.

The next chart shows ***WT*** as a function of ***JT***, in an ideal grid and in a **WRONJ** grid, both with 1000 workers. The ***AT*** value in the **WRONJ** grid is one millisecond. If ***JT*** is 1.5 seconds or is equal to 0.999 seconds (i.e. the limit value ***(W -1) * AT***), then ***WT*** in both grids is very similar. But when ***JT*** is 0.5 second, ***WT*** is twice as long in the **WRONJ** grid than in the ideal grid:
 

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/JTLimit.png)

In a grid with a fixed value ***JT*** for the workloads, we'll flip the previous inequalities:

- If ***W*** <= ***JT / AT + 1*** , then ***WT &#x2243; JT + AT***.
- If ***W*** > ***JT / AT + 1*** , then ***WT &#x2243; W * AT***. 

We define the two limits used before as:
- #### JTL
    -  Job time limit: ***JTL = (W -1) * AT***
- #### WL 
    -  Workers limit: ***WL = JT / AT + 1***

 The next chart shows the scalability problem with **WRONJ** grids. The workload has a million jobs where ***JT*** is a second and a half, the ***AT*** in the **WRONJ** grid is one millisecond, and we are increasing the number of workers: 1000, 1500, 2000. In this case the grid is full, so in the ideal grid, the total time is inversely proportional to ***W***. But in the **WRONJ** grid, once we reach the limit value of 1501 workers (***= WL***), the total time remains at the fixed value of 1000 seconds (***= J * AT***):

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/WLimit.png)


## WWT proof

First, we define these two concepts:

 1. ***tbe*** (time between endings): when a worker finishes its job, it's released and placed at the end of the ***FWQ***. The ***tbe*** is the elapsed time between the moment a worker finishes its job and the moment the last worker that was placed at the ***FWQ*** finished its own job. The ***tbe*** increases when the times of jobs in the active workers increase, or when the number of ***aw*** decreases. The ***tbe*** decreases when the times of jobs in the active workers decrease, or when the number of ***aw*** increases.
 1. ***TBE*** (Time Between Endings): average of all ***tbe*** in the workload. 

If ***JT / W >> AT***, then ***TBE >> AT*** and when there is a idle worker in the ***FWQ***, normally no other worker will finish their job while the ***JS*** assigns a new job to that idle worker. The ***FWQ*** will be empty most of the time, and workers will only wait ***AT*** until they are assigned a new job, so ***WT &#x2243; JT + AT***.

In this case the ***FWQ*** is normally empty, so ***FW &#x2243; 0***, ***AW &#x2243; W*** and ***TBE &#x2243; JT / W***. This is the case of our first example, a full grid with 10 workers and ***AT = 100 ms*** running a workload with ***JT = 5 seconds***. We can see that ***WWT***  tends to ***JT + AT*** (5.1 seconds), and ***TBE*** tends to ***JT / W*** (0.5 seconds):


![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/bigJTSimulation.gif)

When ***JT / W << AT***, normally the ***FWQ*** won't be empty, and ***fw*** will change:
 - When ***fw*** increases, the ***tbe*** increases as well (cause ***aw*** decreases), and when ***at < tbe*** the ***FWQ*** will shrink (it empties faster than it fills), so ***fw*** will decrease.
 - When ***fw*** decreases, the ***tbe*** decreases as well (cause ***aw*** increases), and when ***at > tbe*** the ***FWQ*** will grow (it empties slower than it fills), so ***fw*** will increase.
 
In this way, the grid will tend toward a state of equilibrium where:

 **1)** ***AT = TBE***.

Although the variables ***jt*** and ***at*** are not independent of the variable ***aw***, we assume that these two formulas are valid:

  **2)** ***TBE &#x2243; JT / AW***  
 
  **3)** ***FWQT &#x2243; FW * AT***

And combining that formulas with the previous equality we have:

 **4)** (From **1)** and **2)**): ***AT &#x2243; JT / AW = JT / (W - FW)***
 
 **5)** (From **4)**): ***FW &#x2243; W - JT / AT***

 **6)** (From **3)** and **5)**): ***FWQT &#x2243; W * AT - JT***
 
 and finally, from the definition of ***WT*** and **6)**:

- ***WT = FWQT + JT &#x2243; W * AT***

The ***[JTL](#JTL)*** will be the ***JT*** value that makes equal the two values for ***WT*** :  ***JT + AT = W * AT***:
- ***JT = (W - 1) * AT***


The next samples show the grid dynamics when JT is less than that limit. In the first one we use the same grid as before, with the value of ***JT*** set to 0.5 seconds, so ***WWT*** will tend to ***W * AT*** (1 second), and ***TBE*** will tend to ***&#x2243; AT*** (0.1 seconds):

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/smallJTSimulation.gif)

The second one depicts a more realistic grid, with 100 workers, ***AT = 10 ms*** and ***JT = 0.5 seconds***. In this grid, ***TBE*** will tend to 10 ms while ***WWT*** will tend again to 1 second :

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/simulation100W.gif)



## WWT graphical proof with constant times

When ***at*** and ***jt*** are constant (and therefore ***AT=at*** and ***JT=jt***), then the two formulas for ***WWT*** become equalities. We'll show it using a grid with 10 workers and ***at = 1 second***. 

First, the case where ***JT >= [JTL](#JTL)***. The following simulations are set with ***JT*** equal to the limit ***[JTL](#JTL)*** (9 seconds), 

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/constantSimulation9.gif)

and with ***JT = 20 seconds***:

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/constantSimulation20.gif)

As we can observe, ***WT*** are constant and equal to ***JT + AT*** in both cases, cause whenever a worker finishes its job, it finds the ***FWQ*** is empty and it only has to wait ***at*** for the next job. To better understand the behavior of a worker, check the following time diagrams for our grid of 10 workers:

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/dt_gt_limit.png)

The timeline of each worker is drawn in red when the worker is in the ***FWQ***, and in green when is processing a job. For the first worker only, some arrows are used. An upward pointing arrow is drawn from the time the worker completes a job  to the ***FWQ***, meaning that the worker is placed at the end of the ***FWQ***. When the ***JS*** is ready to assign it a new job, a downward pointing arrow is drawn from ***FWQ*** to the worker's timeline. The time between the arrows pointing down in these diagrams is always ***jt + fwqt***, so this time is the ***wt***. In cases like these, where ***jt*** exceeds the limit, ***fqwt*** is just ***at***. Then ***wt = jt + at*** and is constant because ***jt*** and ***at*** are constant, so:
 
 - ***WT = JT + AT***

 We also see why ***(W - 1) * AT*** is the limit value for ***JT***: with any other lesser value, the ***FWQ*** would not be empty when the worker finished its job.

Now, we'll see two samples where ***JT < [JTL](#JTL)***. The values for ***jt*** are 2 and 5 seconds:

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/constantSimulation2.gif)

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/constantSimulation5.gif)

And here is the correspondent time diagram:

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/dt_lt_limit.png) 

When the first worker queues at ***FWQ***, it has to wait there until the ***JS*** assigns a job to all the subsequent workers. Now we can clearly see the two parts of  ***wt*** :  ***jt*** and ***fwqt***, and how is the sum of these parts constant regardless of ***jt*** (when ***jt*** decreases, ***fqwt*** increases by the same amount) and equal to ***w * at***, so:

-  ***WT = W * AT***

# Possible solutions to the WRONJ problem

No matter how small it may be, ***AT*** will always be greater than zero. To avoid the **WRONJ** problem, the grid should scale the number of ***JS*** threads of execution as it scales the number of workers. A good solution is to create ***JS*** processes that handle a maximum number of workers: this grid will ensure the smooth running of workloads with ***JT*** above a certain minimun, which depends on the maximum number of workers assigned to each ***JS***.

For instance, if we have a grid with 4000 workers, where ***AT*** is 1 ms, and we expect the workloads ***JT*** to not fall bellow 0.5 seconds, 8 ***JS*** should be created (managing 500 workers each).

If we are using a ***WRONJ*** grid whose code we cannot modify, the only solution is to modify the tasks that will be sent to the grid (to make these task heavier), until we pass the grid ***JT*** thresold with our workloads.

# The WRONJ app

This repository contains the code for a [Xamarin](https://dotnet.microsoft.com/apps/xamarin) application, written using [VS 2019](https://visualstudio.microsoft.com/vs/). You can modify, build and run this code if you have that IDE, or you can just download the **WRONJ** app from [Google Play](https://play.google.com/store/apps/details?id=com.wronj) to your Android device.

The **WRONJ** app simulate a virtual grid where we can see the effects of the **WRONJ** problem, by setting the parameters of the grid and those of the workload:

![alt text](https://raw.githubusercontent.com/jorgelasa/wronj/master/Images/WRONJApp.gif)

The volatility fields are used to generate pseudo-random times for ***at*** and ***jt***, according to a log-normal distribution with that volatility and mean equal to the corresponding average time. If the *Random* field is not checked, a fixed value will be used as the seed of the generation.

The *Constant times* values will be always calculated with constant times (volatility=0), and are recalculated every time a basic parameter is changed. The *Variable times* values will be calculated using the volatility values, and only after pressing the button. 

Using the *Charts* and *Simulate* buttons we'll see graphs and grid simulations like the ones we have seen we have seen throughout this document.