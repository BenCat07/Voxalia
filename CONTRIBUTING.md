Contribution to Voxalia
-----------------------

## Issues

See the [ISSUE_TEMPLATE](/ISSUE_TEMPLATE) file for details on posting issues.

## Signing

Please sign [the Contributor License Agreement](https://cla-assistant.io/FreneticLLC/Voxalia).

## Pull Requests

For pull requests:
- You MUST: own the code you submit OR it is free/open/safe to use, EG it is MIT licensed.
	- If your code is under any pre-existing license, you MUST note it in the pull request.
- Any and all code you submit is subject to the mini-license below.
- In general, you should confirm your PR is functional in any ways that it may vary.
	- EG if your code adds a system call, it should be tested across multiple operating systems.
	- Or, if your code adds 3 new item types, all 3 item types should be tested with all relevant item activation methods (EG clicking, etc).
- USUALLY, your pull request should fix an open issue.
	- If there is no issue for it, please open one.
	- If issues for it have been closed with a refusal statement, ensure you want to be making a PR at all before bothering with it. Generally, refused issues means PRs are also refused.
		- The exception to this is when an issue is only partially related to what you're doing, or you believe in good faith that the issue was refused on grounds made irrelevant by your adaptation of it.
			- In these cases, please open a new issue.

## Code Style / Formatting

- For the most part, format under standard Visual Studio rules.
	- Spaces, not tabs.
- Don't use `var`, use explicit types always.
- Don't delete the `using System;` line from the top.
- Prefer to make things public whenever that is safe to do.
- Sample below:

```cs
public void MyMethod(int input)
{
    int myInt = input + 1;
    DoSomething(myInt);
}
```

## Mini-license pre-warning

By contributing to the project, you give up all rights to your contribution.

If you later decide you don't want us using your code - you may make a polite request and it will be treated as such, but that is the extent of your abilities.

### You agree by contributing:

- That you have read and agree to this document.
- To not attempt to "revoke rights to your code" or any similar action.
- That you have the right to publicly contribute any assets/code given. (IE, no contributing someone else's code without their permission)
- That the code submitted is either your own work, dedicated now to this project, OR it is under a license specified directly in the contribution.
