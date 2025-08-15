import { world, system, CustomCommandStatus, CommandPermissionLevel, CustomCommandParamType } from "@minecraft/server";

class CommandSystem {
  static Prefix = "!";
  static Commands = {};

  static RegisterCommand(name, description, callback, paramTypes, opOnly = false) {
    this.Commands[name] = {
      description: description,
      callback: callback,
      paramTypes: paramTypes,
      opOnly: opOnly,
    };
  }

  static executeCommand(input, source, event) {
    if (input.startsWith(CommandSystem.Prefix)) {
      event.cancel = true;
      system.run(() => {
        const text = input.replace(this.Prefix, "");
        const parts = text.split(" ");
        const commandName = parts[0];
        const args = parts.slice(1);

        if (this.Commands[commandName]) {
          const command = this.Commands[commandName];
          if (command.opOnly && !source.isOp()) {
            source.sendMessage("§cThis command can only be used by operators!");
            return;
          }
          const typedArgs = {};

          typedArgs["source"] = source;

          let i = 0;
          for (const [paramName, type] of Object.entries(command.paramTypes)) {
            let value = args[i];

            switch (type) {
              case "string":
                if (value == undefined) {
                  source.sendMessage(
                    `§cInvalid Paramater Input: §e[${paramName}]§r. Received §e[${value}]§r, Expected §e[${type}]§r`
                  );
                  return;
                }
                break;
              case "integer":
                value = parseInt(value);
                if (isNaN(value)) {
                  source.sendMessage(
                    `§cInvalid Paramater Input: §e[${paramName}]§r. Received §e[${value}]§r, Expected §e[${type}]§r`
                  );
                  return;
                }
                break;
              case "float":
                value = parseFloat(value);
                if (isNaN(value)) {
                  source.sendMessage(
                    `§cInvalid Paramater Input: §e[${paramName}]§r. Received §e[${value}]§r, Expected §e[${type}]§r`
                  );
                  return;
                }
                break;
              default:
                source.sendMessage(
                  `§cInvalid parameter type: ${type} for param ${paramName}`
                );
                return;
            }

            typedArgs[paramName] = value;
            i++;
          }
          command.callback.call(null, typedArgs);
        } else {
          source.sendMessage(`§cUnkown command: ${commandName}`);
        }
      });
    }
  }
}

system.afterEvents.scriptEventReceive.subscribe((event) => {
  const {
    id, // returns string (wiki:test)
    initiator, // returns Entity (or undefined if an NPC did not fire the command)
    message, // returns string (Hello World)
    sourceBlock, // returns Block (or undefined if a block did not fire the command)
    sourceEntity, // returns Entity (or undefined if an entity did not fire the command)
    sourceType, // returns MessageSourceType (can be 'Block', 'Entity', 'NPCDialogue', or 'Server')
  } = event;

  // `/scriptevent voicecraft:voice help` -> !help

  if (id.toLowerCase() === 'voicecraft:voice') {
    let source = sourceEntity ?? initiator;
    if (sourceType == 'Server' || source === undefined)  {
      source = {
        isOp: () => true,
        sendMessage: function(message) {
          console.log(message);
        }
      }
    }
    source.sendMessage('* Using /scriptevent is no longer needed. Use "/voicecraft:' + message + '" instead.');
    CommandSystem.executeCommand(CommandSystem.Prefix + message, source, event);
  }
});

// No longer using this, but if beta api's are enabled, it will still work
world?.beforeEvents?.chatSend?.subscribe?.((ev) => {
  CommandSystem.executeCommand(ev.message, ev.sender, ev);
});

system?.beforeEvents?.startup?.subscribe?.((init) => {
  // All commands should have loaded in.
  for (const commandName in CommandSystem.Commands) {
    const { description, callback, paramTypes, opOnly } = CommandSystem.Commands[commandName];

    /** @type {import("@minecraft/server").CustomCommand} */
    const command = {
      name: "voicecraft:" + commandName,
      description,
      permissionLevel: opOnly ? CommandPermissionLevel.Admin : CommandPermissionLevel.Any,
      cheatsRequired: false, // Allow to use without cheats enabled.
      mandatoryParameters: [],
    }

    for (const [paramName, type] of Object.entries(paramTypes)) {
      switch (type.toLowerCase()) {
        case "string":
          command.mandatoryParameters.push({
            type: CustomCommandParamType.String,
            name: paramName,
          });
          break;
        case "integer":
          command.mandatoryParameters.push({
            type: CustomCommandParamType.Integer,
            name: paramName,
          });
          break;
        case "float":
          command.mandatoryParameters.push({
            type: CustomCommandParamType.Float,
            name: paramName,
          });
          break;
        default:
          console.warn(
            `§cInvalid parameter type: ${type} for param ${paramName}`
          );
          return;
      }
    }

    /**
     * @param {import("@minecraft/server").CustomCommandOrigin} origin
     * @returns {import("@minecraft/server").CustomCommandResult}
     */
    init?.customCommandRegistry?.registerCommand?.(command, function(origin, ...args) {
      let source = origin?.sourceEntity ?? origin?.initiator;
      if (origin?.sourceType === 'Server')  {
        source = {
          isOp: () => true,
          sendMessage: function(message) {
            console.log(message);
          }
        }
      }
      if (!source) {
        return {
          status: CustomCommandStatus?.Failure,
          message: 'No valid source found!',
        }
      }

      const typedArgs = { source };
      for (let i = 0; i < Math.min(args.length, command.mandatoryParameters.length); i++) {
        typedArgs[command.mandatoryParameters[i].name] = args[i];
      }

      callback.call(null, typedArgs);
      return {
        status: CustomCommandStatus?.Success,
      };
    });
  }
});

export { CommandSystem };
