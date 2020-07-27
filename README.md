# The WRONJ (Workers Relax On Next Job) problem

The **WRONJ** problem occurs when a [job scheduler](https://en.wikipedia.org/wiki/Job_scheduler) has a single process for assigning new jobs to idle workers. This problem usually goes unnoticed, but may be a big issue in the context of [Grid computing](https://en.wikipedia.org/wiki/Grid_computing). 

In a nutshell, if the grid is often full (i.e. the number of jobs to be done is much greater than the number of workers of the grid) we may want to improve the performance of the grid by increasing the number of workers, but that's when the problem may arise: if the number of workers reaches a certain limit (***= jt / at***, where ***jt*** is the  average time of the jobs and ***at*** is the average time that takes to the scheduler assigning a new job from the job queue to an idle worker ), the grid just don't scale (the performance is the same from that limit on). 

