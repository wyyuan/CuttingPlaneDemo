# 一类网络路径问题的求解实现
[原文链接](http://blog.leanote.com/post/wuyang/916117070645)

这个repo展示的是上述文章中提到的求解问题方法的实现。
- 它依赖于第三方库[C-Sharp-Algorithms](https://github.com/aalhour/C-Sharp-Algorithms)实现对时空状态网络的构建。
- 它实现了三种不同的求解方式：
  - 次梯度算法，参见类 DpnSolver.cs
  - 割平面法，参见类 DpnSolverV3.cs
  - 带有Trust region 的割平面法，参见类 DpnSolverV4.cs
