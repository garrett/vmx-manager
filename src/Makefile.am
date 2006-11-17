
ASSEMBLIES = vmman.exe

asmdir = $(pkglibdir)
asm_DATA = $(ASSEMBLIES)

bin_SCRIPTS = vmman

VMMAN_SOURCE =				\
	ConfigDialog.cs			\
	EntryPoint.cs			\
	MainController.cs		\
	MainWindow.cs			\
	OSModel.cs			\
	Utility.cs			\
	VirtualMachine.cs		\
	VirtualMachineManager.cs	\
	VMModel.cs			\
	VMView.cs

VMMAN_RESOURCES = defaults.vmx operating-systems.csv vmman.glade vmman.xml

VMMAN_EXPANDED_RESOURCES = $(foreach resource,$(VMMAN_RESOURCES), $(top_srcdir)/data/$(resource))

VMMAN_EXPANDED_RESOURCES_BUILD = $(foreach resource,$(VMMAN_EXPANDED_RESOURCES), $(addprefix -resource:,$(resource)),$(notdir $(resource)))

vmman.exe: $(VMMAN_SOURCE) $(VMMAN_EXPANDED_RESOURCES)
	$(MCS) -out:$@ $(VMMAN_SOURCE) $(VMMAN_EXPANDED_RESOURCES_BUILD) $(VMMAN_LIBS)

CLEANFILES = $(bin_SCRIPTS) $(ASSEMBLIES)
