using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Janphe.Fantasy.Map
{
    partial class Map6Religions
    {
        // name generation approach and relative chance to be selected
        static readonly JObject approach = JObject.Parse(@"{
          'Number': 1, 'Being': 3, 'Adjective': 5, 'Color + Animal': 5,
          'Adjective + Animal': 5, 'Adjective + Being': 5, 'Adjective + Genitive': 1,
          'Color + Being': 3, 'Color + Genitive': 3, 'Being + of + Genitive': 2, 'Being + of the + Genitive': 1,
          'Animal + of + Genitive': 1, 'Adjective + Being + of + Genitive': 2, 'Adjective + Animal + of + Genitive': 2
        }");
        static readonly IList<string> approaches = approach.dict().ww();

        static readonly JObject @base = JObject.Parse(@"{
          number: ['One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine', 'Ten', 'Eleven', 'Twelve'],
          being: ['God', 'Goddess', 'Lord', 'Lady', 'Deity', 'Creator', 'Maker', 'Overlord', 'Ruler', 'Chief', 'Master', 'Spirit', 'Ancestor', 'Father', 'Forebear', 'Forefather', 'Mother', 'Brother', 'Sister', 'Elder', 'Numen', 'Ancient', 'Virgin', 'Giver', 'Council', 'Guardian', 'Reaper'],
          animal: ['Dragon', 'Wyvern', 'Phoenix', 'Unicorn', 'Sphinx', 'Centaur', 'Pegasus', 'Kraken', 'Basilisk', 'Chimera', 'Cyclope', 'Antelope', 'Ape', 'Badger', 'Bear', 'Beaver', 'Bison', 'Boar', 'Buffalo', 'Cat', 'Cobra', 'Crane', 'Crocodile', 'Crow', 'Deer', 'Dog', 'Eagle', 'Elk', 'Fox', 'Goat', 'Goose', 'Hare', 'Hawk', 'Heron', 'Horse', 'Hyena', 'Ibis', 'Jackal', 'Jaguar', 'Lark', 'Leopard', 'Lion', 'Mantis', 'Marten', 'Moose', 'Mule', 'Narwhal', 'Owl', 'Panther', 'Rat', 'Raven', 'Rook', 'Scorpion', 'Shark', 'Sheep', 'Snake', 'Spider', 'Swan', 'Tiger', 'Turtle', 'Viper', 'Vulture', 'Walrus', 'Wolf', 'Wolverine', 'Worm', 'Camel', 'Falcon', 'Hound', 'Ox', 'Serpent'],
          adjective: ['New', 'Good', 'High', 'Old', 'Great', 'Big', 'Young', 'Major', 'Strong', 'Happy', 'Last', 'Main', 'Huge', 'Far', 'Beautiful', 'Wild', 'Fair', 'Prime', 'Crazy', 'Ancient', 'Proud', 'Secret', 'Lucky', 'Sad', 'Silent', 'Latter', 'Severe', 'Fat', 'Holy', 'Pure', 'Aggressive', 'Honest', 'Giant', 'Mad', 'Pregnant', 'Distant', 'Lost', 'Broken', 'Blind', 'Friendly', 'Unknown', 'Sleeping', 'Slumbering', 'Loud', 'Hungry', 'Wise', 'Worried', 'Sacred', 'Magical', 'Superior', 'Patient', 'Dead', 'Deadly', 'Peaceful', 'Grateful', 'Frozen', 'Evil', 'Scary', 'Burning', 'Divine', 'Bloody', 'Dying', 'Waking', 'Brutal', 'Unhappy', 'Calm', 'Cruel', 'Favorable', 'Blond', 'Explicit', 'Disturbing', 'Devastating', 'Brave', 'Sunny', 'Troubled', 'Flying', 'Sustainable', 'Marine', 'Fatal', 'Inherent', 'Selected', 'Naval', 'Cheerful', 'Almighty', 'Benevolent', 'Eternal', 'Immutable', 'Infallible'],
          genitive: ['Day', 'Life', 'Death', 'Night', 'Home', 'Fog', 'Snow', 'Winter', 'Summer', 'Cold', 'Springs', 'Gates', 'Nature', 'Thunder', 'Lightning', 'War', 'Ice', 'Frost', 'Fire', 'Doom', 'Fate', 'Pain', 'Heaven', 'Justice', 'Light', 'Love', 'Time', 'Victory'],
          theGenitive: ['World', 'Word', 'South', 'West', 'North', 'East', 'Sun', 'Moon', 'Peak', 'Fall', 'Dawn', 'Eclipse', 'Abyss', 'Blood', 'Tree', 'Earth', 'Harvest', 'Rainbow', 'Sea', 'Sky', 'Stars', 'Storm', 'Underworld', 'Wild'],
          color: ['Dark', 'Light', 'Bright', 'Golden', 'White', 'Black', 'Red', 'Pink', 'Purple', 'Blue', 'Green', 'Yellow', 'Amber', 'Orange', 'Brown', 'Grey']
        }");

        static readonly JObject forms = JObject.Parse(@"{
          Folk: {'Shamanism': 2, 'Animism': 2, 'Ancestor worship': 1, 'Polytheism': 2},
          Organized: {'Polytheism': 5, 'Dualism': 1, 'Monotheism': 4, 'Non-theism': 1},
          Cult: {'Cult': 1, 'Dark Cult': 1},
          Heresy: {'Heresy': 1}
        }");

        static readonly JObject methods = JObject.Parse(@"{
          'Random + type': 3,
          'Random + ism': 1,
          'Supreme + ism': 5,
          'Faith of + Supreme': 5,
          'Place + ism': 1,
          'Culture + ism': 2,
          'Place + ian + type': 6,
          'Culture + type': 4
        }");

        static readonly JObject types = JObject.Parse(@"{
          'Shamanism': {'Beliefs': 3, 'Shamanism': 2, 'Spirits': 1},
          'Animism': {'Spirits': 1, 'Beliefs': 1},
          'Ancestor worship': {'Beliefs': 1, 'Forefathers': 2, 'Ancestors': 2},
          'Polytheism': {'Deities': 3, 'Faith': 1, 'Gods': 1, 'Pantheon': 1},

          'Dualism': {'Religion': 3, 'Faith': 1, 'Cult': 1},
          'Monotheism': {'Religion': 1, 'Church': 1},
          'Non-theism': {'Beliefs': 3, 'Spirits': 1},

          'Cult': {'Cult': 4, 'Sect': 4, 'Worship': 1, 'Orden': 1, 'Coterie': 1, 'Arcanum': 1},
          'Dark Cult': {'Cult': 2, 'Sect': 2, 'Occultism': 1, 'Idols': 1, 'Coven': 1, 'Circle': 1, 'Blasphemy': 1},

          'Heresy': {
            'Heresy': 3,
            'Sect': 2,
            'Schism': 1,
            'Dissenters': 1,
            'Circle': 1,
            'Brotherhood': 1,
            'Society': 1,
            'Iconoclasm': 1,
            'Dissent': 1,
            'Apostates': 1
          }
        }");
    }
}
