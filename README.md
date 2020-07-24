# The WRONJ (Waiting On Next Job) problem

The **WRONJ** problem occurs when a [job scheduler](https://en.wikipedia.org/wiki/Job_scheduler) has a single process for assigning new jobs to idle workers. This problem usually goes unnoticed, but may be a big issue in the context of [Grid computing](https://en.wikipedia.org/wiki/Grid_computing): when the number of workers reaches a certain limit (dependent on assignments and jobs times), the grid just don't scale.
